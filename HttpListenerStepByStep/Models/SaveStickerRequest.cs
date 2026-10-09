namespace HttpListenerStepByStep.Models;

public class SaveStickerRequest
{
    public required string Text { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}