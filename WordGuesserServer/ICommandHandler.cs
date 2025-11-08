using System.Collections.Concurrent;
using System.Net.Sockets;

namespace WordGuesserServer;

public interface ICommandHandler
{
    Task Invoke(Socket sender, ConcurrentDictionary<Guid, GameSession> gameSessions, byte[]? payload = null, CancellationToken ct = default);
}