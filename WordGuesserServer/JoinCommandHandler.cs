using WordGuesser.Protocol;
using System.Net.Sockets;

namespace WordGuesserServer;

[Command(Command.Join)]
public class JoinCommandHandler :  ICommandHandler
{
    public async Task Invoke(Socket sender, GameSessionRegistry gameSessions, byte[]? payload = null, CancellationToken ct = default)
    {
        var sessionId = GuessCommandHelper.Decode<Guid>(Command.Join, payload!);

        var response = gameSessions.TryJoin(sessionId, sender, out var gameSession);
        if (response != CommandResponse.Ok)
        {
            await sender.SendResponseCommand(response, ct);
            return;
        }

        await sender.SendSessionId(sessionId, ct);
        await sender.SendPacketAsync(GuessCommandHelper.Ready(PlayerRole.Guesser),
            ct);
        
        await gameSession!.Hosted.Connector.SendPacketAsync(GuessCommandHelper.Ready(PlayerRole.Hosted),
            ct);
    }
}
