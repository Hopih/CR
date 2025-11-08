using System.Net.Sockets;

namespace WordGuesserServer;

public class GameSession
{
    public required Guid Id { get; set; }
    public Player? Player { get; set; }
    public required Player Hosted { get; set; }
    public string? GuessedWord { get; set; }
    public int TryCount { get; set; } = 5;
    
    public SessionState State { get; set; } = SessionState.WaitingForPlayer;
}