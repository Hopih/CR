using SimpleHttpListenerServer.Entities;

namespace SimpleHttpListenerServer.Database;

public interface IBoardsRepository
{
    Task<(Guid boardId, Guid tempKey, DateTime expires)> CreateGuestBoardAsync(string title, TimeSpan ttl,
        CancellationToken ct);

    Task<Guid> CreateUserBoardAsync(long ownerUserId, string title, CancellationToken ct);

    Task<List<Board>> GetUserBoardsAsync(long userId, CancellationToken ct);

    Task<(Guid Id, int? OwnerUserId, bool IsTemporary, Guid? TempKey, DateTime? TempExpiresAt)?> GetBoardMetaAsync(
        Guid boardId, CancellationToken ct);

    Task<int> DeleteExpiredGuestBoardsAsync(CancellationToken ct);

}