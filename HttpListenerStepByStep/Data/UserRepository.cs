using System.Data;
using HttpListenerStepByStep.Models;
using Npgsql;
using NpgsqlTypes;

namespace HttpListenerStepByStep.Data;

public sealed class UserRepository(string connectionString) : BaseRepository(connectionString)
{
    public async Task<UserProfile> CreateAsync(string login, string name,
        string passwordHash, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("public.create_user", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        
        command.Parameters.AddWithValue("p_login", NpgsqlDbType.Varchar, login);
        command.Parameters.AddWithValue("p_display_name", NpgsqlDbType.Varchar, name);
        command.Parameters.AddWithValue("p_password_hash", NpgsqlDbType.Text, passwordHash);
        command.Parameters.Add(new NpgsqlParameter("p_id", NpgsqlDbType.Uuid)
        {
            Direction = ParameterDirection.Output
        });
        
        var id = (Guid)(await command.ExecuteScalarAsync(ct)
                        ?? throw new InvalidOperationException("Не получен UUID"));
        
        await using var read = new NpgsqlCommand(
            "SELECT * FROM public.get_user_profile($1)", connection);
        
        read.Parameters.AddWithValue(id);
        
        await using var reader = await read.ExecuteReaderAsync(ct);
        
        if (!await reader.ReadAsync(ct))
        {
            throw new InvalidOperationException("Пользователь не найден");
        }

        return new UserProfile(
            reader.GetGuid(0), 
            reader.GetString(1), 
            reader.GetString(2));
    }
}