namespace HttpListenerStepByStep.Models;


public sealed record BoardResponse(
    Guid Id, string Title, DateTime CreatedAt,
    IReadOnlyList<Sticker> Stickers);