namespace WebServerExample;

[AttributeUsage(AttributeTargets.Method)]
public class Mapping : Attribute
{
    public string Route;
    public string Method;
    public string Role;
    public Mapping(string route, string method, string role = "any")
    {
        Route = route;
        Method = method;
        Role = role;
    }
}