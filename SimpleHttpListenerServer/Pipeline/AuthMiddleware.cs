using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Helpers;

namespace SimpleHttpListenerServer.Pipeline;

public sealed class AuthMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContextEx ctx, Func<Task> next)
    {
        ctx.GuestToken = ctx.Request.Headers["X-Guest-Token"] ?? ctx.Request.Cookies?["guest"]?.Value;
        
        var user = ctx.Request.GetUser();
        ctx.AuthUser = user;
        ctx.IsAuthenticated = user != null;
        
        return next();
    }
}