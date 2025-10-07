using System.Data;
using Npgsql;
using SimpleHttpListenerServer.Entities;
using SimpleHttpListenerServer.Helpers;

namespace SimpleHttpListenerServer.Database;

public sealed class UsersRepository
{
    private readonly string _cs;
    public UsersRepository(string cs) => _cs = cs;

    public async Task<int?> CreateUserAsync(string login, string password, CancellationToken ct)
    {
        var hashPass = MyPasswordHasher.Hash(password).Split(";");
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync(ct);

        var sql = @"insert into app_user(login, pwd_hash, pwd_salt, role)
                    values(@login, @hash, @salt, 'user')
                    returning id;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("login", login);
        cmd.Parameters.AddWithValue("hash", hashPass[0]);
        cmd.Parameters.AddWithValue("salt", hashPass[1]);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is int id ? id : null;
    }

    public sealed class UserRow
    {
        public int Id { get; init; }
        public string Login { get; init; } = "";
        public string Hash { get; init; } = "";
        public string Salt { get; init; } = "";
        public string Role { get; init; } = "user";
    }

    public async Task<UserRow?> GetByLoginAsync(string login, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync(ct);

        var sql = @"select id, login, pwd_hash, pwd_salt, role from app_user where login=@login;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("login", login);

        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (await rd.ReadAsync(ct))
        {
            return new UserRow {
                Id = rd.GetInt32(0),
                Login = rd.GetString(1),
                Hash = rd.GetString(2),
                Salt = rd.GetString(3),
                Role = rd.GetString(4)
            };
        }
        return null;
    }
}