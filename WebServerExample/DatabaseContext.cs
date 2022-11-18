using System.Data;
using Dapper;
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
        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        var id = Guid.NewGuid();
        var  sqlExpression=
            $@"INSERT INTO Users (Id, Email, Mobile, Description, Password) VALUES 
            (
             '{id}',
             '{user.Email}'
             ,'{user.Mobile}'
             ,'{user.Description}'
             ,'{user.Password}')";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sqlExpression, connection);
        var result = await command.ExecuteNonQueryAsync();
        if (result <= 0)
        {
            return null;
        }

        user.Id = id;
        return user;

    }

    public async Task<User> GetUser(LoginModel loginModel)
    {
        var sqlExpression = $@"select * from users where Email = '{loginModel.Email}'";
        using IDbConnection db = new SqlConnection(_connectionString);
        var user = await db.QuerySingleOrDefaultAsync<User>(sqlExpression);

        return user != null && BCrypt.Net.BCrypt.Verify(loginModel.Password, user.Password) 
            ? user 
            : null;
    }
    
    public async Task<User> GetUserById(string id)
    {
        var sqlExpression = $@"select * from users where Id = '{id.ToUpper()}'";
        using IDbConnection db = new SqlConnection(_connectionString);
        var user = await db.QuerySingleOrDefaultAsync<User>(sqlExpression);

        return user;
    }
}