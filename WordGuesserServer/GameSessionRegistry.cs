using System.Net.Sockets;
using WordGuesser.Protocol;

namespace WordGuesserServer;

public sealed class GameSessionRegistry
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, GameSession> _sessions = new();
    private readonly Dictionary<Socket, GameSession> _members = new();

    public int Count { get { lock (_sync) return _sessions.Count; } }

    public GameSession? Find(Guid id)
    {
        lock (_sync) return _sessions.GetValueOrDefault(id);
    }

    public GameSession? FindByPlayer(Socket socket)
    {
        lock (_sync) return _members.GetValueOrDefault(socket);
    }

    public CommandResponse TryCreate(Socket socket, out GameSession? session)
    {
        lock (_sync)
        {
            session = null;
            if (_members.ContainsKey(socket)) return CommandResponse.AlreadyInSession;
            session = new GameSession
            {
                Id = Guid.NewGuid(),
                Hosted = new Player { Connector = socket, Role = PlayerRole.Hosted }
            };
            _sessions.Add(session.Id, session);
            _members.Add(socket, session);
            return CommandResponse.Ok;
        }
    }

    // Called by a handler while ExecuteAsync holds the target session's gate.
    public CommandResponse TryJoin(Guid id, Socket socket, out GameSession? session)
    {
        lock (_sync)
        {
            session = null;
            if (_members.ContainsKey(socket)) return CommandResponse.AlreadyInSession;
            if (!_sessions.TryGetValue(id, out session)) return CommandResponse.SessionNotFound;
            if (session.Player != null) return CommandResponse.SessionFull;
            session.Player = new Player { Connector = socket, Role = PlayerRole.Guesser };
            session.State = SessionState.Ready;
            _members.Add(socket, session);
            return CommandResponse.Ok;
        }
    }

    public async Task ExecuteAsync(Command command, Socket sender, byte[] payload, CancellationToken ct)
    {
        var handler = CommandHandlerFactory.GetHandler(command);
        var session = command switch
        {
            Command.Join => Find(GuessCommandHelper.Decode<Guid>(command, payload)),
            Command.Word or Command.Guess => Find(GuessCommandHelper.DecodeSessionWithData(command, payload).SessionId),
            _ => FindByPlayer(sender)
        };

        if (session == null)
        {
            await handler.Invoke(sender, this, payload, ct);
            return;
        }

        await session.Gate.WaitAsync(ct);
        try
        {
            // The handler looks up the session again: it may have ended while we were waiting.
            await handler.Invoke(sender, this, payload, ct);
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            // A send may fail on the opponent's socket. Notify every still-reachable participant.
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await EndAsync(session, ct: cleanup.Token);
            throw;
        }
        finally { session.Gate.Release(); }
    }

    // Called under the session gate. Remove membership before any network operation.
    public async Task EndAsync(GameSession session, Socket? disconnected = null, CancellationToken ct = default)
    {
        Socket[] players;
        lock (_sync)
        {
            if (!_sessions.Remove(session.Id)) return;
            session.State = SessionState.Finished;
            players = session.Player == null
                ? [session.Hosted.Connector]
                : [session.Hosted.Connector, session.Player.Connector];
            foreach (var player in players) _members.Remove(player);
        }

        foreach (var player in players)
        {
            if (ReferenceEquals(player, disconnected) || ct.IsCancellationRequested) continue;
            try { await player.SendPacketAsync(GuessCommandHelper.End(), ct); }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
            {
                // One unavailable player must not prevent cleanup or notification of the other.
                player.Dispose();
            }
        }
    }

    public async Task DisconnectAsync(Socket socket, bool notifyOpponent)
    {
        var session = FindByPlayer(socket);
        if (session == null) return;
        await session.Gate.WaitAsync();
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            if (!notifyOpponent) timeout.Cancel();
            await EndAsync(session, socket, timeout.Token);
        }
        finally { session.Gate.Release(); }
    }
}
