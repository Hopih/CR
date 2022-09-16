namespace WebServerExample;

[AttributeUsage(AttributeTargets.Method)]
public class Mapping : Attribute
{
    public string Route;
    public string Method;
    public Mapping(string route, string method)
    {
        Route = route;
        Method = method;
    }
}