using System.Data;
using Microsoft.Data.SqlClient;
using WebServerExample.Models;

namespace WebServerExample;

public class DatabaseContext
{
    private readonly string _connectionString;
    public DatabaseContext(string server, string dbName)
    {
        _connectionString = $"Server={server};Database={dbName};Encrypt=False;Integrated Security=True;TrustServerCertificate=True;";
    }

   
    public async Task<User> AddUser(User user)
    {
        var id = Guid.NewGuid();
        var sqlExpression =
            $@"INSERT INTO Users 
            (Id, Email, Mobile, Description) 
            VALUES (
                    '{id}',
                    '{user.Email}',
                    '{user.Mobile}',
                    '{user.Description}'
                    )";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync();
            await using var command = new SqlCommand(sqlExpression, connection);
            var result = await command.ExecuteNonQueryAsync();
            if (result > 0)
            {
                user.Id = id;
                return user;
            }
        }
        finally
        {
            await connection.CloseAsync();
        }
        return null;
    }

    public async Task<User> GetUser(Guid id)
    {
        var sqlExpression = $@"Select * from Users where Id = '{id}'";
        
        await using var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync();
            /*
            await using var command2 = new SqlCommand();
            command2.Parameters.Add(new SqlParameter("id", SqlDbType.UniqueIdentifier){Value = id});
            command2.CommandType = CommandType.StoredProcedure;
            command2.Connection = connection;
            var resultStorage = await command2.ExecuteReaderAsync();
            */
            await using var command = new SqlCommand(sqlExpression, connection);
            var result = await command.ExecuteReaderAsync();
            if (result is { HasRows: true })
            {
                return new User
                {
                    Id = result.GetGuid("Id"),
                    Description = result.GetString("Description"),
                    Email = result.GetString("Email"),
                    Mobile = result.GetString("Mobile")
                };
            }
        }
        finally
        {
            await connection.CloseAsync();
        }

        return null;
    }
}