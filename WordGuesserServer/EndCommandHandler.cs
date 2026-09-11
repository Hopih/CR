using WordGuesser.Protocol;
using System.Net.Sockets;

namespace WordGuesserServer;

[Command(Command.End)]
public class EndCommandHandler: ICommandHandler
{
    public async Task Invoke(
        Socket sender,
        GameSessionRegistry sessions,
        byte[]? payload = null,
        CancellationToken ct = default)
    {
        var session = sessions.FindByPlayer(sender);

        if (session == null)
        {
            await sender.SendSessionNotFound(ct);
            return;
        }

        await sessions.EndAsync(session, ct: ct);
    }
}
