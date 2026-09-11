using WordGuesser.Protocol;
using System.Net.Sockets;

namespace WordGuesserServer;

[Command(Command.Create)]
public class CreateCommandHandler : ICommandHandler
{
    public async Task Invoke(Socket sender, GameSessionRegistry gameSessions, byte[]? payload = null, CancellationToken ct = default)
    {
        var response = gameSessions.TryCreate(sender, out var session);
        if (response != CommandResponse.Ok)
        {
            await sender.SendResponseCommand(response, ct);
            return;
        }
        await sender.SendSessionId(session!.Id, ct);
    }
}
