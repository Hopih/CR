using System.Net;

namespace HttpListenerStepByStep.Http;

public class Router
{
    private sealed record Entry(
        string Method,
        string[] Segments,
        string Template,
        RequestHandler Handler);

    private readonly List<Entry> _entries = [];

    public void Map(string method, string template, RequestHandler handler)
    {
        method = method.ToUpperInvariant();
        template = Normalize(template);
        if (_entries.Any(x => x.Method == method && x.Template == template))
            throw new InvalidOperationException("Маршрут уже зарегистрирован");
        _entries.Add(new(method, Split(template), template, handler));
    }

// Чистый поиск маршрута: можно проверить без запуска HttpListener.
    public RouteMatch? Match(string method, string path)
    {
        var segments = Split(Normalize(path));
        var allowed = new List<string>();
        foreach (var entry in _entries)
        {
            if (entry.Segments.Length != segments.Length) continue;
            var values = new Dictionary<string, string>();
            var matches = true;
            for (var i = 0; i < segments.Length; i++)
            {
                var pattern = entry.Segments[i];
                if (pattern.StartsWith('{') && pattern.EndsWith('}'))
                {
                    values[pattern[1..^1]] = segments[i];
                }
                else if (pattern != segments[i])
                {
                    matches = false;
                    break;
                }
            }

            if (!matches) continue;
            allowed.Add(entry.Method);
            if (entry.Method == method)
                return new(entry.Handler, values, []);
        }

        return allowed.Count == 0
            ? null
            : new(null, new Dictionary<string, string>(),
                allowed.Distinct().Order().ToArray());
    }

    public async Task<bool> TryHandleAsync(HttpListenerContext context, 
        CancellationToken ct)
    {
        var match = Match(context.Request.HttpMethod,
            context.Request.Url!.AbsolutePath);
        if (match is null)
        {
            return false;
        }

        if (match.Handler is null)
        {
            context.Response.Headers["Allow"] = string.Join(", ", match.AllowedMethods);
            throw new ApiException(405, "method_not_allowed", "Метод не разрешён");
        }

        await match.Handler(new(context, match.Values), ct);
        return true;
    }

    private static string Normalize(string path) =>
        path == "/" ? path : path.TrimEnd('/');

    private static string[] Split(string path) =>
        path == "/" ? [] : path.TrimStart('/').Split('/');
}