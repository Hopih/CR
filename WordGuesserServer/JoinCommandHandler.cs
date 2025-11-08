using System.Collections.Concurrent;
using System.Net.Sockets;

namespace WordGuesserServer;

[Command(Command.Join)]
public class JoinCommandHandler :  ICommandHandler
{
    public async Task Invoke(Socket sender, ConcurrentDictionary<Guid, GameSession> gameSessions, byte[]? payload = null, CancellationToken ct = default)
    {
        var sessionId = GuessCommandHelper.Decode<Guid>(Command.Join, payload!);

        if (!gameSessions.TryGetValue(sessionId, out var gameSession))
        {
            await sender.SendSessionNotFound();
            return;
        }

        gameSession.Player = new Player()
        {
            Connector = sender,
            Role = PlayerRole.Guesser
        };
        gameSession.Hosted.Role = PlayerRole.Hosted;
        
        await sender.SendAsync(new GuessPackageBuilder(
                    GuessCommandHelper.Ready(PlayerRole.Guesser), Command.Ready).Build(),
            SocketFlags.None);
        
        await gameSession.Hosted.Connector.SendAsync(new GuessPackageBuilder(
                GuessCommandHelper.Ready(PlayerRole.Hosted), Command.Ready).Build(),
            SocketFlags.None);
    }
}