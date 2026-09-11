using System.Net.Sockets;
using WordGuesser.Protocol;

namespace WordGuesserServer;

[Command(Command.Swap)]
public class SwapCommandHandler : ICommandHandler
{
    public Task Invoke(Socket sender, GameSessionRegistry sessions, byte[]? payload = null, CancellationToken ct = default) =>
        sender.SendResponseCommand(sessions.FindByPlayer(sender) == null
            ? CommandResponse.SessionNotFound : CommandResponse.InvalidState, ct);
}
