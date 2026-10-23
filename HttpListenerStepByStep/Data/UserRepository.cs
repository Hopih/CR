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
    
    public Task<User?> FindByLoginAsync(string login, CancellationToken ct) =>
        FindAsync("SELECT * FROM public.get_login_user($1)", login, ct);
    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) =>
        FindAsync("SELECT * FROM public.get_session_user($1)", id, ct);
    private async Task<User?> FindAsync(string sql, object value, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter
        {
            NpgsqlDbType = value is Guid ? NpgsqlDbType.Uuid : NpgsqlDbType.Varchar,
            Value = value
        });
        
        await using var reader = await command.ExecuteReaderAsync(ct);
        
        return !await reader.ReadAsync(ct)
            ? null
            : new User(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4));
    }
    public async Task RehashAsync(Guid id, string oldHash, string hash, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("public.update_user_hash", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        
        command.Parameters.AddWithValue("p_id", NpgsqlDbType.Uuid, id);
        command.Parameters.AddWithValue("p_old_hash", NpgsqlDbType.Text, oldHash);
        command.Parameters.AddWithValue("p_hash", NpgsqlDbType.Text, hash);
        
        command.Parameters.Add(new NpgsqlParameter("p_found", NpgsqlDbType.Boolean)
        {
            Direction = ParameterDirection.Output
        });
        
        await command.ExecuteScalarAsync(ct);
    }

}