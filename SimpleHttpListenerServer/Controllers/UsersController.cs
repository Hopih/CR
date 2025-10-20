using Npgsql;
using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Entities;
using SimpleHttpListenerServer.Helpers;
using SimpleHttpListenerServer.Models;

namespace SimpleHttpListenerServer.Controllers;

public sealed class UsersController(UsersRepository users) : BaseController
{
    [HttpPost("/api/user/signup")]
    [Authorize(AllowAnonymous = true)]
    public async Task<IActionResult> Signup(SignupRequest dto)
    {
        try
        {
            var userId = await users.CreateUserAsync(dto.Login, dto.Password, HttpContext.RequestAborted);
            if (userId is null)
            {
                return Json(
                    new
                    {
                        error = "signup_failed"
                    });
            }
            
            return Created(
                new 
                { 
                    id = userId 
                });
        }
        catch (PostgresException pg) when (pg.SqlState == "23505")
        {
            return Conflict("login_already_exists");
        }
    }

    [HttpPost("/api/user/login")]
    [Authorize(AllowAnonymous = true)]
    public async Task<IActionResult> Login(LoginRequest dto)
    {
        var row = await users.GetByLoginAsync(dto.Login, HttpContext.RequestAborted);
        if (row is null || !MyPasswordHasher.Validate($"{row.Hash};{row.Salt}", dto.Password))
        {
            return Unauthorized(new { error = "invalid_credentials" });
        }

        var token = JwtWorker.GenerateJwtToken(new User
        {
            Id = row.Id, 
            Role = row.Role
        });
        
        return Ok(new LoginResponse
        {
            Token = token, 
            UserId = row.Id, 
            Role = row.Role
        });
    }
}