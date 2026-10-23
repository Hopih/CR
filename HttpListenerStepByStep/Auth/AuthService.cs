using System.Net;
using HttpListenerStepByStep.Data;
using HttpListenerStepByStep.Http;
using HttpListenerStepByStep.Models;

namespace HttpListenerStepByStep.Auth;

public sealed record CurrentUser(User User, SessionData Session);
public sealed class AuthService(UserRepository users, SessionStore sessions, SessionCookie cookie)
{
    public async Task<CurrentUser> RequireAsync(HttpListenerContext context, CancellationToken ct)
    {
        context.Response.Headers["Cache-Control"] = "no-store";
        var token = context.Request.Cookies[SessionCookie.Name]?.Value;
        var session = sessions.Find(token);
        var user = session is null 
            ? null 
            : await users.FindByIdAsync(session.UserId, ct);

        if (user is not null)
        {
            return new CurrentUser(user, session!);
        }

        sessions.Remove(token); 
        cookie.Clear(context.Response);
            
        throw new ApiException(401, "login_required", "Войдите в аккаунт");
    }
}