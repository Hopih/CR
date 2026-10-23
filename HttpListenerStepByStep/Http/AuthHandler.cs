using FluentValidation;
using HttpListenerStepByStep.Auth;
using HttpListenerStepByStep.Data;
using HttpListenerStepByStep.Models;
using Microsoft.AspNetCore.Identity;

namespace HttpListenerStepByStep.Http;

public sealed class AuthHandler(
    UserRepository users,
    SessionStore sessions,
    SessionCookie cookie,
    AuthService auth,
    IValidator<LoginRequest> validator,
    PasswordHasher<object> hasher)
{
    private readonly string _dummy = hasher.HashPassword(new object(), Guid.NewGuid().ToString());

    public async Task LoginAsync(RouteContext route, CancellationToken ct)
    {
        var input = await route.Http.Request.ReadAsync<LoginRequest>(ct);
        input = input with
        {
            Login = input.Login?.Trim().ToLowerInvariant()
        };
        
        input.Validate(validator);
        
        var user = await users.FindByLoginAsync(input.Login!, ct);
        var result = hasher.VerifyHashedPassword(new object(), user?.PasswordHash ?? _dummy, input.Password!);
        
        if (user is null || result == PasswordVerificationResult.Failed)
        {
            throw new ApiException(401, "invalid_credentials", "Неверный логин или пароль");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            await users.RehashAsync(user.Id, user.PasswordHash,
                hasher.HashPassword(new object(), input.Password!), ct);
        }

        sessions.Remove(route.Http.Request.Cookies[SessionCookie.Name]?.Value);
        var issued = sessions.Create(user.Id);
        cookie.Set(route.Http.Response, issued.Token);
        route.Http.Response.Headers["Cache-Control"] = "no-store";
        await route.Http.Response.WriteAsync(200, new { user = user.Public(),
            absoluteExpiresAt = issued.Data.AbsoluteExpiresAt }, ct);
    }
    public async Task MeAsync(RouteContext route, CancellationToken ct)
    {
        var actor = await auth.RequireAsync(route.Http, ct);
        
        await route.Http.Response.WriteAsync(200,
            new
            { 
                user = actor.User.Public(), 
                absoluteExpiresAt = actor.Session.AbsoluteExpiresAt 
            }, ct);
    }
    public async Task LogoutAsync(RouteContext route, CancellationToken ct)
    {
        sessions.Remove(route.Http.Request.Cookies[SessionCookie.Name]?.Value);
        cookie.Clear(route.Http.Response);
        route.Http.Response.Headers["Cache-Control"] = "no-store";
        await route.Http.Response.WriteAsync(204, ct: ct);
    }
}