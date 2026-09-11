using System.Net.Sockets;
using WordGuesser.Protocol;

namespace WordGuesserServer;

[Command(Command.Guess)]
public class GuessCommandHandler : ICommandHandler
{
    public async Task Invoke(Socket sender, GameSessionRegistry sessions, byte[]? payload = null, CancellationToken ct = default)
    {
        var (id, guess) = GuessCommandHelper.DecodeSessionWithData(Command.Guess, payload!);
        var session = sessions.Find(id);
        if (session == null) { await sender.SendSessionNotFound(ct); return; }
        var guesser = session.Player;
        if (guesser == null || !ReferenceEquals(guesser.Connector, sender))
        { await sender.SendIncorrectSender(ct); return; }
        var result = GameRules.Guess(session, guess, out var mask, out var guessed);
        if (result != CommandResponse.Ok) { await sender.SendResponseCommand(result, ct); return; }

        var host = session.Hosted;
        var attempt = GameEventCodec.Attempt(session.TryCount, mask);
        await host.Connector.SendPacketAsync(attempt, ct);
        await guesser.Connector.SendPacketAsync(attempt, ct);
        if (session.State != SessionState.RoundEnd) return;

        var winner = guessed ? guesser : host;
        winner.WinCount++;
        await host.Connector.SendPacketAsync(GameEventCodec.Round(!guessed, host.WinCount, guesser.WinCount,
            session.TryCount, session.GuessedWord!), ct);
        await guesser.Connector.SendPacketAsync(GameEventCodec.Round(guessed, guesser.WinCount, host.WinCount,
            session.TryCount, session.GuessedWord!), ct);

        if (winner.WinCount == GameRules.WinningScore)
        {
            await host.Connector.SendPacketAsync(GameEventCodec.Match(!guessed, host.WinCount, guesser.WinCount), ct);
            await guesser.Connector.SendPacketAsync(GameEventCodec.Match(guessed, guesser.WinCount, host.WinCount), ct);
            await sessions.EndAsync(session, ct: ct);
            return;
        }

        (session.Hosted, session.Player) = (guesser, host);
        guesser.Role = PlayerRole.Hosted;
        host.Role = PlayerRole.Guesser;
        session.GuessedWord = null;
        session.TryCount = GameRules.AttemptsPerRound;
        session.State = SessionState.Ready;
        await guesser.Connector.SendPacketAsync(GuessCommandHelper.Ready(PlayerRole.Hosted), ct);
        await host.Connector.SendPacketAsync(GuessCommandHelper.Ready(PlayerRole.Guesser), ct);
    }
}
