using SimpleHttpListenerServer.Core.MiniOrm;

namespace SimpleHttpListenerServer.Entities;

[Table("board")]
public sealed class BoardEntity
{
    [Key, Column("id")] public Guid Id { get; set; }

    [Column("owner_user_id")] public int? OwnerUserId { get; set; }
    [Column("title")] public string Title { get; set; } = null!;
    [Column("is_temporary")] public bool IsTemporary { get; set; }
    [Column("temp_key")] public Guid? TempKey { get; set; }
    [Column("temp_expires_at")] public DateTime? TempExpiresAt { get; set; }
}