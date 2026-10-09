namespace HttpListenerStepByStep.Models;

public sealed class Sticker
{
    public Guid Id { get; set; } 
    public Guid BoardId { get; set; } 
    public string? Text { get; set; } 
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } 
    public int Height { get; set; } 
    public DateTime CreatedAt { get; set; } 
}