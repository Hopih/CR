namespace SimpleHttpListenerServer.Core;

public abstract class HttpVerbAttribute(string method, string template) : Attribute
{
    public string Template { get; } = template;
    public string Method { get; } = method;
}
public sealed class HttpGetAttribute(string template) : HttpVerbAttribute("GET", template);
public sealed class HttpPostAttribute(string template) : HttpVerbAttribute("POST", template);

[Flags]
public enum AuthTokenKind { None = 0, Guest = 1, Jwt = 2 }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class AuthorizeAttribute : Attribute
{
    public bool AllowAnonymous { get; init; } = false;
    public AuthTokenKind Required { get; init; } = AuthTokenKind.Jwt;
    public string? Role { get; init; }
}