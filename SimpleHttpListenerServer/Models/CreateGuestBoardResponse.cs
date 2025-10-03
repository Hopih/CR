namespace SimpleHttpListenerServer.Models;

public class CreateGuestBoardResponse
{
    public Guid BoardId { get; set; } 
    public Guid TempKey { get; set; } 
    public DateTime ExpiresAt { get; set; }
}