using System.Reflection;
using System.Text.RegularExpressions;

namespace SimpleHttpListenerServer.Core;

public sealed class RouteEntry
{
    public string Method { get; init; } = default!;
    public string Template { get; init; } = default!;
    public Regex Regex { get; init; } = default!;
    public Type ControllerType { get; init; } = default!;
    public MethodInfo Action { get; init; } = default!;
    public AuthorizeAttribute? Auth { get; init; }
    public ParameterInfo? ModelParam { get; init; } // если метод принимает один параметр-модель
    public string[] ParamNames { get; init; } = [];
}


public sealed partial class RouteTable
{
    private readonly List<RouteEntry> _routes = new();

    public IEnumerable<RouteEntry> All => _routes;

    public void BuildFromControllers(IEnumerable<Type> controllerTypes)
    {
        _routes.Clear();
        foreach (var type in controllerTypes)
        {
            var classAuth = type.GetCustomAttribute<AuthorizeAttribute>();
            foreach (var m in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                var httpAttrs = m.GetCustomAttributes<HttpVerbAttribute>(true).ToArray();
                if (httpAttrs.Length == 0) continue;

                var methodAuth = m.GetCustomAttribute<AuthorizeAttribute>();
                var effectiveAuth = methodAuth ?? classAuth;

                // упрощённый шаблон: /api/boards/{id} -> регексп
                foreach (var ha in httpAttrs)
                {
                    var (rx, names) = TemplateToRegex(ha.Template);
                    var modelParam = m.GetParameters()
                        .FirstOrDefault(p =>
                            p.ParameterType != typeof(HttpContextEx) &&
                            p.ParameterType != typeof(CancellationToken) &&
                            !p.ParameterType.IsPrimitive &&
                            p.ParameterType != typeof(string) &&
                            !p.ParameterType.IsEnum &&
                            !p.ParameterType.IsValueType); // исключаем Guid, int, bool, etc.

                    _routes.Add(new RouteEntry
                    {
                        Method = ha.Method,
                        Template = ha.Template,
                        Regex = rx,
                        ParamNames = names,
                        ControllerType = type,
                        Action = m,
                        Auth = effectiveAuth,
                        ModelParam = modelParam
                    });
                }
            }
        }
    }

    public (RouteEntry entry, Dictionary<string,string> routeValues)? Match(string path, string method)
    {
        foreach (var r in _routes.Where(r => r.Method.Equals(method, StringComparison.OrdinalIgnoreCase)))
        {
            var m = r.Regex.Match(path);
            if (!m.Success) continue;
            var dict = new Dictionary<string,string>();
            for (var i = 0; i < r.ParamNames.Length; i++)
                dict[r.ParamNames[i]] = m.Groups[i+1].Value;
            return (r, dict);
        }
        return null;
    }

    private static (Regex, string[]) TemplateToRegex(string template)
    {
        // /api/boards/{id}/elements/{elementId}
        var names = new List<string>();
        var pattern = RouteRegex().Replace(template, m => {
            names.Add(m.Groups[1].Value);
            return "([^/]+)";
        });
        return (new Regex("^" + pattern + "$", RegexOptions.Compiled | RegexOptions.IgnoreCase), names.ToArray());
    }

    [GeneratedRegex("{([^}/]+)}")]
    private static partial Regex RouteRegex();
}