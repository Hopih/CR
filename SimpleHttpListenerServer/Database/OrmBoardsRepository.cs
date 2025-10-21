using SimpleHttpListenerServer.Core.MiniOrm;
using SimpleHttpListenerServer.Entities;

namespace SimpleHttpListenerServer.Database;

public class OrmBoardsRepository(MiniOrmContext orm) : IBoardsRepository
{
    public async Task<(Guid boardId, Guid tempKey, DateTime expires)> CreateGuestBoardAsync(string title, TimeSpan ttl, CancellationToken ct)
    {
        var board = new BoardEntity
        {
            Id = Guid.NewGuid(),
            Title = title,
            IsTemporary = true,
            TempKey = Guid.NewGuid(),
            TempExpiresAt = DateTime.UtcNow.Add(ttl),
        };

        await orm.InsertAsync(board, ct);
        return (board.Id, board.TempKey!.Value, board.TempExpiresAt!.Value);
    }

    public async Task<Guid> CreateUserBoardAsync(long ownerUserId, string title, CancellationToken ct)
    {
        var board = new BoardEntity
        {
            Id = Guid.NewGuid(),
            OwnerUserId = (int)ownerUserId,
            Title = title,
            IsTemporary = false,
        };

        await orm.InsertAsync(board, ct);
        
        return board.Id;
    }

    public async Task<List<Board>> GetUserBoardsAsync(long userId, CancellationToken ct)
    {
        var builder = orm.From<BoardEntity>()
            .Where(b => b.OwnerUserId == (int)userId)
            .OrderByDescending(b => b.TempExpiresAt);

        var entities = await orm.QueryAsync(builder, ct);

        // Преобразуем в старый DTO (Board), чтобы контракт не менялся
        var boards = entities.Select(e => new Board
        {
            Id = e.Id,
            OwnerUserId = e.OwnerUserId,
            Title = e.Title,
            IsTemporary = e.IsTemporary,
            TempKey = e.TempKey,
            TempExpiresAt = e.TempExpiresAt,
        }).ToList();

        return boards;
    }

    public async Task<(Guid Id, int? OwnerUserId, bool IsTemporary, Guid? TempKey, DateTime? TempExpiresAt)?>
        GetBoardMetaAsync(Guid boardId, CancellationToken ct)
    {
        var builder = orm.From<BoardEntity>()
            .Where(b => b.Id == boardId);

        var rows = await orm.QueryAsync(builder, ct);
        var b = rows.FirstOrDefault();
        return b is null
            ? null
            : (b.Id, b.OwnerUserId, b.IsTemporary, b.TempKey, b.TempExpiresAt);
    }

    public async Task<int> DeleteExpiredGuestBoardsAsync(CancellationToken ct)
    {
        var expired = await orm.QueryAsync(
            orm.From<BoardEntity>()
                .Where(b => b.IsTemporary && b.TempExpiresAt < DateTime.UtcNow),
            ct);

        var affected = 0;
        foreach (var b in expired)
        {
            await orm.DeleteAsync<BoardEntity>(b.Id, ct);
            affected++;
        }

        return affected;
    }
}