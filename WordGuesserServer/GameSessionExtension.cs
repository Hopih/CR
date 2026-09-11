using System.Net.Sockets;

namespace WordGuesserServer;

public static class GameSessionExtension
{
    public static async Task<(bool HasSession, GameSession? Session)> CheckSessionById(this GameSessionRegistry gameSessions,
        Guid sessionId, Socket sender, CancellationToken ct = default)
    {
        var gameSession = gameSessions.Find(sessionId);
        if (gameSession != null)
        {
            return (true,  gameSession);
        }

        await sender.SendSessionNotFound(ct);
        
        return (false, null);
    }

    public static async Task<bool> CheckSessionHost(this GameSession gameSession, Socket sender, CancellationToken ct = default)
    {
        if (gameSession.Hosted.Connector.Equals(sender))
        {
            return true;
        }

        await sender.SendIncorrectSender(ct);
        
        return false;
    }

    public static async Task<bool> CheckPlayer(this GameSession gameSession, Socket sender, CancellationToken ct = default)
    {
        if (gameSession.Player != null)
        {
            return true;
        }

        await sender.SendPlayerNotJoined(ct);
        
        return false;
    }
}
