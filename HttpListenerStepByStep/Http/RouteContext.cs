using System.Net;

namespace HttpListenerStepByStep.Http;

public sealed class RouteContext(HttpListenerContext http,
    IReadOnlyDictionary<string, string> values)
{
    public Guid Id(string name) => ApiSupport.ParseId(Values[name]);
    public HttpListenerContext Http { get; init; } = http;
    public IReadOnlyDictionary<string, string> Values { get; init; } = values;
}
public delegate Task RequestHandler(RouteContext route, CancellationToken ct);