using WordGuesser.Protocol;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace WordGuesserServer;

[Command(Command.End)]
public class EndCommandHandler: ICommandHandler
{
    public async Task Invoke(
        Socket sender,
        ConcurrentDictionary<Guid, GameSession> sessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        var session = sessions.Values.FirstOrDefault(s =>
            s.Hosted.Connector.Equals(sender) || (s.Player?.Connector.Equals(sender) ?? false));

        if (session == null)
        {
            await sender.SendSessionNotFound();
            return;
        }

        await session.Hosted.Connector.SendPacketAsync(
            GuessCommandHelper.End(), ct);

        if (session.Player != null)
            await session.Player.Connector.SendPacketAsync(
                GuessCommandHelper.End(), ct);

        sessions.TryRemove(session.Id, out _);
    }
}