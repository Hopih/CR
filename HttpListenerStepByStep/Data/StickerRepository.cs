using System.Data;
using HttpListenerStepByStep.Models;
using Npgsql;
using NpgsqlTypes;

namespace HttpListenerStepByStep.Data;

public class StickerRepository(string connectionString)
    : BaseRepository(connectionString)
{
    public async Task<IReadOnlyList<Sticker>> GetByBoardAsync(
        Guid boardId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            "SELECT * FROM public.get_board_stickers($1)", connection);
        command.Parameters.Add(new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlDbType.Uuid, Value = boardId
        });
        
        var items = new List<Sticker>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        
        while (await reader.ReadAsync(ct))
        {
            items.Add(new Sticker
            {
                Id = reader.GetGuid(0),
                BoardId = reader.GetGuid(1),
                Text = reader.GetString(2),
                X = reader.GetInt32(3),
                Y = reader.GetInt32(4),
                Width = reader.GetInt32(5),
                Height = reader.GetInt32(6),
                CreatedAt = reader.GetDateTime(7)
            });
        }
        return items;
    }

    public async Task<Guid> CreateAsync(
        Guid boardId, SaveStickerRequest value, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = Procedure("public.create_sticker", connection);
        
        command.Parameters.AddWithValue("p_board_id", NpgsqlDbType.Uuid, boardId);
        AddFields(command, value);
        AddOutput(command, "p_id", NpgsqlDbType.Uuid);
        
        return await command.ExecuteScalarAsync(ct) is Guid id
            ? id
            : throw new InvalidOperationException("Не получен UUID стикера");
    }

    public async Task<bool> UpdateAsync(
        Guid boardId, Guid stickerId,
        SaveStickerRequest value, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = Procedure("public.update_sticker", connection);
        
        AddIds(command, boardId, stickerId);
        AddFields(command, value);
        AddOutput(command, "p_found", NpgsqlDbType.Boolean);
        
        return await ReadFoundAsync(command, ct);
    }

    public async Task<bool> DeleteAsync(
        Guid boardId, Guid stickerId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = Procedure("public.delete_sticker", connection);
        
        AddIds(command, boardId, stickerId);
        AddOutput(command, "p_found", NpgsqlDbType.Boolean);
        
        return await ReadFoundAsync(command, ct);
    }

    private static NpgsqlCommand Procedure(
        string name, NpgsqlConnection connection) => new(name, connection)
    {
        CommandType = CommandType.StoredProcedure
    };

    private static void AddOutput(
        NpgsqlCommand command, string name, NpgsqlDbType type)
    {
        command.Parameters.Add(new NpgsqlParameter(name, type)
        {
            Direction = ParameterDirection.Output
        });
    }

    private static void AddIds(
        NpgsqlCommand command, Guid boardId, Guid stickerId)
    {
        command.Parameters.AddWithValue(
            "p_board_id", NpgsqlDbType.Uuid, boardId);
        command.Parameters.AddWithValue(
            "p_sticker_id", NpgsqlDbType.Uuid, stickerId);
    }

    private static void AddFields(NpgsqlCommand command, SaveStickerRequest value)
    {
        command.Parameters.AddWithValue("p_text", NpgsqlDbType.Varchar, value.Text);
        command.Parameters.AddWithValue("p_x", NpgsqlDbType.Integer, value.X);
        command.Parameters.AddWithValue("p_y", NpgsqlDbType.Integer, value.Y);
        command.Parameters.AddWithValue("p_width", NpgsqlDbType.Integer, value.Width);
        command.Parameters.AddWithValue("p_height", NpgsqlDbType.Integer, value.Height);
    }

    private static async Task<bool> ReadFoundAsync(
        NpgsqlCommand command, CancellationToken ct) =>
        await command.ExecuteScalarAsync(ct) is bool found
            ? found
            : throw new InvalidOperationException("Не получен p_found");
}