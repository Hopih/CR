using System.Collections.Concurrent;
using System.Net.Sockets;

namespace WordGuesserServer;

public static class GameSessionExtension
{
    public static async Task<(bool HasSession, GameSession? Session)> CheckSessionById(this ConcurrentDictionary<Guid, GameSession> gameSessions,
        Guid sessionId, Socket sender)
    {
        if (gameSessions.TryGetValue(sessionId, out var gameSession))
        {
            return (true,  gameSession);
        }

        await sender.SendSessionNotFound();
        
        return (false, null);
    }

    public static async Task<bool> CheckSessionHost(this GameSession gameSession, Socket sender)
    {
        if (gameSession.Hosted.Connector.Equals(sender))
        {
            return true;
        }

        await sender.SendIncorrectSender();
        
        return false;
    }

    public static async Task<bool> CheckPlayer(this GameSession gameSession, Socket sender)
    {
        if (gameSession.Player != null)
        {
            return true;
        }

        await sender.SendPlayerNotJoined();
        
        return false;
    }
}