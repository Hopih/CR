using System.Data;
using System.Net;
using Npgsql;
using SimpleHttpListenerServer.Entities;

namespace SimpleHttpListenerServer;

public class DbContext
{
    private const string DbConnectionString = "Host=localhost;Username=postgres;Password=postgres;Database=white_board";
    private readonly NpgsqlConnection _dbConnection = new(DbConnectionString);


    public async Task<User> CreateUser(string login, string password, CancellationToken cancellationToken = default)
    {
        await _dbConnection.OpenAsync(cancellationToken);
        try
        {
            const string sqlQuery = "INSERT INTO users (login, password) VALUES (@login, @password) RETURNING id;";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("login", login);
            cmd.Parameters.AddWithValue("password", password);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            return new User
            {
                Login = login,
                Password = password,
                Id = Convert.ToInt32(result)
            };
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }


    public async Task<User?> GetUser(string login, CancellationToken cancellationToken = default)
    {
        await _dbConnection.OpenAsync(cancellationToken);
        try
        {
            const string sqlQuery = "SELECT * FROM users WHERE login = @login and password = @password";
            var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
            cmd.Parameters.AddWithValue("login", login);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if(reader.HasRows && await reader.ReadAsync(cancellationToken))
            {
                return new User
                {
                    Id = reader.GetInt64("id"),
                    Password = reader.GetString("password"),
                    Role = reader.GetString("role"),
                };
            }
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }

        return null;
    }
}