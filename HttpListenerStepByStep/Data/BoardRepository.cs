using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace HttpListenerStepByStep.Data;

public sealed class BoardRepository(string connectionString)
    : BaseRepository(connectionString)
{
    public async Task<Guid> CreateAsync(string title, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            $"public.create_board", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("p_text", NpgsqlDbType.Varchar, title);
        command.Parameters.AddWithValue("p_id", NpgsqlDbType.Uuid, null);
        
        await using var reader = await command.ExecuteReaderAsync(ct);
        return !await reader.ReadAsync(ct)
            ? throw new InvalidOperationException("Процедура не вернула id")
            : reader.GetGuid(0);
    }
}