using Npgsql;
using SimpleHttpListenerServer.Entities;

namespace SimpleHttpListenerServer.Database;

public sealed class ElementsRepository
{
    NpgsqlDataSource _dataSource;
    public ElementsRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<List<BoardElement>> GetElementsWithStickersAsync(Guid boardId, CancellationToken ct)
    {
        var result = new List<BoardElement>();
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var sql = @"
select
  e.id, e.board_id, e.user_id, e.type, e.x, e.y, e.width, e.height, e.z_index, e.rotation, e.updated_at,
  s.text, s.color, s.font_size
from board_element e
left join sticker s on s.element_id = e.id and e.type = 'sticker'
where e.board_id = @board_id
order by e.z_index, e.updated_at;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("board_id", boardId);

        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            result.Add(new BoardElement {
                Id = rd.GetGuid(0),
                BoardId = rd.GetGuid(1),
                UserId = rd.IsDBNull(2) ? null : rd.GetInt32(2),
                Type = Enum.Parse<ElementType>(rd.GetString(3), true),
                X = rd.GetInt32(4),
                Y = rd.GetInt32(5),
                Width = rd.GetInt32(6),
                Height = rd.GetInt32(7),
                ZIndex = rd.GetInt32(8),
                Rotation = rd.GetInt32(9),
                UpdatedAt = rd.GetDateTime(10),
                Text = rd.IsDBNull(11) ? null : rd.GetString(11),
                Color = rd.IsDBNull(12) ? null : rd.GetString(12),
                FontSize = rd.IsDBNull(13) ? null : rd.GetInt32(13)
            });
        }
        return result;
    }

    public async Task UpsertElementsAsync(Guid boardId, int? userId, IEnumerable<BoardElement> items, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        const string upsertElement = """

                                     insert into board_element (id, board_id, user_id, type, x, y, width, height, z_index, rotation, updated_at)
                                     values (@id, @board_id, @user_id, @type, @x, @y, @width, @height, @z_index, @rotation, now())
                                     on conflict (id) do update
                                     set x = excluded.x, y = excluded.y,
                                         width = excluded.width, height = excluded.height,
                                         z_index = excluded.z_index, rotation = excluded.rotation,
                                         updated_at = now();
                                     """;

        const string upsertSticker = """

                                     insert into sticker (element_id, text, color, font_size)
                                     values (@id, @text, @color, @font_size)
                                     on conflict (element_id) do update
                                     set text = excluded.text, color = excluded.color, font_size = excluded.font_size;
                                     """;

        foreach (var it in items)
        {
            await using (var cmd = new NpgsqlCommand(upsertElement, conn, tx))
            {
                cmd.Parameters.AddWithValue("id", it.Id);
                cmd.Parameters.AddWithValue("board_id", boardId);
                cmd.Parameters.AddWithValue("user_id", (object?)userId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("type", it.Type);
                cmd.Parameters.AddWithValue("x", it.X);
                cmd.Parameters.AddWithValue("y", it.Y);
                cmd.Parameters.AddWithValue("width", it.Width);
                cmd.Parameters.AddWithValue("height", it.Height);
                cmd.Parameters.AddWithValue("z_index", it.ZIndex);
                cmd.Parameters.AddWithValue("rotation", it.Rotation);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            if (it.Type == ElementType.Sticker)
            {
                await using var cmd2 = new NpgsqlCommand(upsertSticker, conn, tx);
                cmd2.Parameters.AddWithValue("id", it.Id);
                cmd2.Parameters.AddWithValue("text",  (object?)it.Text ?? "");
                cmd2.Parameters.AddWithValue("color", (object?)it.Color ?? "#ffe58a");
                cmd2.Parameters.AddWithValue("font_size", it.FontSize ?? 16);
                await cmd2.ExecuteNonQueryAsync(ct);
            }
        }

        await tx.CommitAsync(ct);
    }
    
    
  public async Task SyncBoardElementsAsync(Guid boardId, int? userId, IEnumerable<BoardElement> newItems, CancellationToken ct)
{
    await using var conn = await _dataSource.OpenConnectionAsync(ct);
    await using var tx = await conn.BeginTransactionAsync(ct);

    try
    {
        // --- 1️⃣ Сохраняем базовые элементы ---
        const string upsertElementSql = """
            INSERT INTO board_element (id, board_id, user_id, type, x, y, width, height, rotation, z_index)
            VALUES (@id, @boardId, @userId, @type, @x, @y, @width, @height, @rotation, @zIndex)
            ON CONFLICT (id) DO UPDATE
            SET
                x = EXCLUDED.x,
                y = EXCLUDED.y,
                width = EXCLUDED.width,
                height = EXCLUDED.height,
                rotation = EXCLUDED.rotation,
                z_index = EXCLUDED.z_index;
        """;

        await using (var upsertCmd = new NpgsqlCommand(upsertElementSql, conn, tx))
        {
            foreach (var e in newItems)
            {
                upsertCmd.Parameters.Clear();
                upsertCmd.Parameters.AddWithValue("id", e.Id);
                upsertCmd.Parameters.AddWithValue("boardId", boardId);
                upsertCmd.Parameters.AddWithValue("userId", (object?)userId ?? DBNull.Value);
                upsertCmd.Parameters.AddWithValue("type", e.Type);
                upsertCmd.Parameters.AddWithValue("x", e.X);
                upsertCmd.Parameters.AddWithValue("y", e.Y);
                upsertCmd.Parameters.AddWithValue("width", e.Width);
                upsertCmd.Parameters.AddWithValue("height", e.Height);
                upsertCmd.Parameters.AddWithValue("rotation", e.Rotation);
                upsertCmd.Parameters.AddWithValue("zIndex", e.ZIndex);
                await upsertCmd.ExecuteNonQueryAsync(ct);
            }
        }

        // --- 2️⃣ Сохраняем доп. данные для стикеров ---
        var stickers = newItems.Where(e => e.Type == ElementType.Sticker).ToList();
        if (stickers.Count > 0)
        {
            const string upsertStickerSql = """
                INSERT INTO sticker (element_id, text, color, font_size)
                VALUES (@id, @text, @color, @fontSize)
                ON CONFLICT (element_id) DO UPDATE
                SET text = EXCLUDED.text,
                    color = EXCLUDED.color,
                    font_size = EXCLUDED.font_size;
            """;

            await using (var stickerCmd = new NpgsqlCommand(upsertStickerSql, conn, tx))
            {
                foreach (var s in stickers)
                {
                    stickerCmd.Parameters.Clear();
                    stickerCmd.Parameters.AddWithValue("id", s.Id);
                    stickerCmd.Parameters.AddWithValue("text", s.Text ?? (object)DBNull.Value);
                    stickerCmd.Parameters.AddWithValue("color", s.Color ?? (object)DBNull.Value);
                    stickerCmd.Parameters.AddWithValue("fontSize", s.FontSize ?? (object)DBNull.Value);
                    await stickerCmd.ExecuteNonQueryAsync(ct);
                }
            }
        }

        // --- 3️⃣ Удаляем отсутствующие ---
        var idsToKeep = newItems.Select(e => e.Id).ToArray();
        await using (var deleteCmd = new NpgsqlCommand("""
            DELETE FROM board_element 
            WHERE board_id = @boardId AND id <> ALL(@idsToKeep)
        """, conn, tx))
        {
            deleteCmd.Parameters.AddWithValue("boardId", boardId);
            deleteCmd.Parameters.AddWithValue("idsToKeep", idsToKeep);
            await deleteCmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }
    catch
    {
        await tx.RollbackAsync(ct);
        throw;
    }
}
}