namespace SimpleHttpListenerServer.Entities;

public sealed class BoardElement 
{
    public Guid Id { get; init; }
    public Guid BoardId { get; init; }
    public int? UserId { get; init; }
    public ElementType Type { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int ZIndex { get; init; }
    public int Rotation { get; init; }
    public DateTime UpdatedAt { get; init; }
    // Sticker payload (nullable)
    public string? Text { get; init; }
    public string? Color { get; init; }
    public int? FontSize { get; init; }
    
    // только для стрелок
    public Guid? FromId { get; set; }  // Sticker A
    public Guid? ToId { get; set; }    // Sticker B
    public double? Bend { get; set; }  // смещение изгиба
}