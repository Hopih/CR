using System.ComponentModel.DataAnnotations;

namespace SimpleHttpListenerServer.Entities;

public sealed class Board 
{
    public Guid Id { get; init; }
    public int? OwnerUserId { get; init; }
    public string Title { get; init; } = "";
    public bool IsTemporary { get; init; }
    public Guid? TempKey { get; init; }
    public DateTime? TempExpiresAt { get; init; }
}
