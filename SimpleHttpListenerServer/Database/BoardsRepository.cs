using Npgsql;
using SimpleHttpListenerServer.Entities;

namespace SimpleHttpListenerServer.Database;

public sealed class BoardsRepository
{
    private readonly string _cs;
    public BoardsRepository(string connectionString) => _cs = connectionString;

    public async Task<(Guid boardId, Guid tempKey, DateTime expires)> CreateGuestBoardAsync(string title, TimeSpan ttl, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync(ct);

        var sql = @"insert into board (title, is_temporary, temp_key, temp_expires_at)
                    values (@title, true, gen_random_uuid(), now() + @ttl)
                    returning id, temp_key, temp_expires_at;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("title", title);
        cmd.Parameters.AddWithValue("ttl", NpgsqlTypes.NpgsqlDbType.Interval, ttl);

        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (await rd.ReadAsync(ct))
        {
            return (rd.GetGuid(0), rd.GetGuid(1), rd.GetDateTime(2));
        }
        throw new InvalidOperationException("Failed to create guest board");
    }

    public async Task<Guid> CreateUserBoardAsync(long ownerUserId, string title, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync(ct);

        var sql = @"insert into board (id, owner_user_id, title, is_temporary)
                    values (gen_random_uuid(), @uid, @title, false)
                    returning id;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", ownerUserId);
        cmd.Parameters.AddWithValue("title", title);

        var id = (Guid)(await cmd.ExecuteScalarAsync(ct) ?? throw new InvalidOperationException());
        return id;
    }

    public async Task<(Guid Id, int? OwnerUserId, bool IsTemporary, Guid? TempKey, DateTime? TempExpiresAt)?> GetBoardMetaAsync(Guid boardId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync(ct);

        var sql = @"select id, owner_user_id, is_temporary, temp_key, temp_expires_at from board where id=@id;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", boardId);

        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (await rd.ReadAsync(ct))
        {
            return (
                rd.GetGuid(0),
                rd.IsDBNull(1) ? null : rd.GetInt32(1),
                rd.GetBoolean(2),
                rd.IsDBNull(3) ? null : rd.GetGuid(3),
                rd.IsDBNull(4) ? null : rd.GetDateTime(4)
            );
        }
        return null;
    }

    public async Task<List<Board>> GetUserBoardsAsync(long userId, CancellationToken ct)
    {
        var list = new List<Board>();
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync(ct);

        var sql = @"select id, owner_user_id, title, is_temporary, temp_key, temp_expires_at, created_at
                    from board where owner_user_id=@uid order by created_at desc;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);

        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            list.Add(new Board {
                Id = rd.GetGuid(0),
                OwnerUserId = rd.IsDBNull(1) ? null : rd.GetInt32(1),
                Title = rd.GetString(2),
                IsTemporary = rd.GetBoolean(3),
                TempKey = rd.IsDBNull(4) ? null : rd.GetGuid(4),
                TempExpiresAt = rd.IsDBNull(5) ? null : rd.GetDateTime(5)
            });
        }
        return list;
    }
}