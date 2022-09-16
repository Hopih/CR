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
            $"INSERT INTO Users (Id, Email, Mobile, Description) VALUES ('{id}','{user.Email}','{user.Mobile}','{user.Description}')";

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
}