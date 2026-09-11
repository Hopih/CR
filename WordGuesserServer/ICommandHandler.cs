using System.Net.Sockets;

namespace WordGuesserServer;

public interface ICommandHandler
{
    Task Invoke(Socket sender, GameSessionRegistry gameSessions, byte[]? payload = null, CancellationToken ct = default);
}