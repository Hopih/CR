using SimpleHttpListenerServer.Entities;

namespace SimpleHttpListenerServer.Models;

public class ElementDto
{
    public Guid Id { get; set; }
    public ElementType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int ZIndex { get; set; }
    public int Rotation { get; set; }

    // sticker payload
    public string? Text { get; set; }
    public string? Color { get; set; }
    public int? FontSize { get; set; }
}