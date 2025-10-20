using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Helpers;

namespace SimpleHttpListenerServer.Pipeline;

public sealed class AuthMiddleware : BaseMiddleware
{
    protected override Task InvokeCoreAsync(HttpContextEx ctx, Func<Task> next, CancellationToken ct)
    {
        ctx.GuestToken = ctx.Request.Headers["X-Guest-Token"] ?? ctx.Request.Cookies["guest_board_key"]?.Value;
        
        var user = ctx.Request.GetUser();
        ctx.AuthUser = user;
        ctx.IsAuthenticated = user != null;
        
        return next();
    }
}