using System.Net;
using SimpleHttpListenerServer.Entities;

namespace SimpleHttpListenerServer;

public static class AuthHelper
{
    public static User? GetUserFromRequest(HttpListenerRequest request)
    {
        var authHeader = request.Headers["Authorization"];
        if (string.IsNullOrEmpty(authHeader)) return null;

        if (!authHeader.StartsWith("Bearer ")) return null;

        var token = authHeader.Substring("Bearer ".Length).Trim();
        var (isSuccess, user) = JwtWorker.ValidateJwtToken(token);

        return isSuccess ? user : null;
    }
}
