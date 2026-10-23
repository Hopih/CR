using System.Data;
using HttpListenerStepByStep.Models;
using Npgsql;
using NpgsqlTypes;

namespace HttpListenerStepByStep.Data;

public sealed class BoardOwnerRepository(string connectionString) : BaseRepository(connectionString)
{
    public async Task<bool> CanAccessAsync(Guid boardId, User user, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT public.can_access_board($1,$2,$3)", connection);
        command.Parameters.AddWithValue(boardId);
        command.Parameters.AddWithValue(user.Id);
        command.Parameters.AddWithValue(user.Role == "Admin");
        
        return (bool)(await command.ExecuteScalarAsync(ct))!;
    }
    public async Task<Guid> CreateAsync(string title, Guid userId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("public.create_owned_board", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        
        command.Parameters.AddWithValue("p_title", NpgsqlDbType.Varchar, title);
        command.Parameters.AddWithValue("p_owner_id", NpgsqlDbType.Uuid, userId);
        command.Parameters.Add(new NpgsqlParameter("p_id", NpgsqlDbType.Uuid)
        {
            Direction = ParameterDirection.Output
        });
        
        return (Guid)(await command.ExecuteScalarAsync(ct))!;
    }
    public async Task<IReadOnlyList<object>> ListAsync(Guid userId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT * FROM public.get_user_boards($1)", connection);
        command.Parameters.AddWithValue(userId);
        
        var items = new List<object>();
        
        await using var reader = await command.ExecuteReaderAsync(ct);
        
        while (await reader.ReadAsync(ct))
        {
            items.Add(new { id=reader.GetGuid(0), title=reader.GetString(1) });
        }

        return items;
    }
}