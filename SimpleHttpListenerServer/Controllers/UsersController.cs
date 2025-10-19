using Npgsql;
using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Entities;
using SimpleHttpListenerServer.Helpers;
using SimpleHttpListenerServer.Models;

namespace SimpleHttpListenerServer.Controllers;

public sealed class UsersController(UsersRepository users)
{
    [HttpPost("/api/user/signup")]
    [Authorize(AllowAnonymous = true)]
    public async Task<object> Signup(SignupRequest dto, HttpContextEx ctx)
    {
        try
        {
            var userId = await users.CreateUserAsync(dto.Login, dto.Password, ctx.RequestAborted);
            if (userId is null)
            {
                return new
                {
                    error = "signup_failed"
                };
            }

            ctx.Response.StatusCode = 201;
            return new
            {
                id = userId
            };

        }
        catch (PostgresException pg) when (pg.SqlState == "23505")
        {
            ctx.Response.StatusCode = 409;
            return new
            {
                error = "login_already_exists"
            };
        }
    }

    [HttpPost("/api/user/login")]
    [Authorize(AllowAnonymous = true)]
    public async Task<object> Login(LoginRequest dto, HttpContextEx ctx)
    {
        var row = await users.GetByLoginAsync(dto.Login, ctx.RequestAborted);
        if (row is null || !MyPasswordHasher.Validate($"{row.Hash};{row.Salt}", dto.Password))
        {
            return new { error = "invalid_credentials" };
        }

        var token = JwtWorker.GenerateJwtToken(new User { Id = row.Id, Role = row.Role });
        
        return new LoginResponse
        {
            Token = token, 
            UserId = row.Id, 
            Role = row.Role
        };
    }
}