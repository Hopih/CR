using System.Net;

namespace HttpListenerStepByStep.Auth;

public sealed class SessionCookie(bool secure)
{
    public const string Name = "board_session";
    private string Value(string token) =>
        $"{Name}={token}; Path=/; HttpOnly; SameSite=Lax{(secure ? "; Secure" : "")}";
    
    public void Set(HttpListenerResponse response, string token) =>
        response.Headers.Add("Set-Cookie", Value(token));
    
    public void Clear(HttpListenerResponse response) =>
        response.Headers.Add("Set-Cookie", $"{Value("")}; Max-Age=0");
}