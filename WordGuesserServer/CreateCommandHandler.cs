using System.Collections.Concurrent;
using System.Net.Sockets;

namespace WordGuesserServer;

[Command(Command.Create)]
public class CreateCommandHandler : ICommandHandler
{
    public async Task Invoke(Socket sender, ConcurrentDictionary<Guid, GameSession> gameSessions, byte[]? payload = null, CancellationToken ct = default)
    {
        var session = new GameSession
        {
            Hosted = new Player
            {
                Connector = sender
            },
            Id = Guid.NewGuid()
        };
        gameSessions.TryAdd(session.Id, session);
        
        await sender.SendAsync(new GuessPackageBuilder(
                    GuessCommandHelper.SendSessionId(session.Id), Command.SessionId)
                .Build(),
            SocketFlags.None);
    }
}