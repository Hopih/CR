namespace HttpListenerStepByStep.Models;

public sealed class Board
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}