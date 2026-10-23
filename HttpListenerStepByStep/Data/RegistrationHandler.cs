using FluentValidation;
using HttpListenerStepByStep.Http;
using HttpListenerStepByStep.Models;
using Microsoft.AspNetCore.Identity;

namespace HttpListenerStepByStep.Data;

public sealed class RegistrationHandler(UserRepository users,
    IValidator<RegisterRequest> validator, PasswordHasher<object> hasher)
{
    public async Task RegisterAsync(RouteContext route, CancellationToken ct)
    {
        var input = await route.Http.Request.ReadAsync<RegisterRequest>(ct);
        input.Login = input.Login?.Trim().ToLowerInvariant();
        input.DisplayName = input.DisplayName?.Trim();
        input.Validate(validator);
        
        var hash = hasher.HashPassword(new object(), input.Password!);
        var user = await users.CreateAsync(input.Login!, input.DisplayName!, hash, ct);
        
        route.Http.Response.Headers["Cache-Control"] = "no-store";
        
        await route.Http.Response.WriteAsync(201, user, ct);
    }
}