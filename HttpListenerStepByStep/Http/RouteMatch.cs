namespace HttpListenerStepByStep.Http;

public sealed class RouteMatch(RequestHandler? handler,
    IReadOnlyDictionary<string, string> values, string[] allowedMethods)
{
    public RequestHandler? Handler { get; init; } = handler;
    public IReadOnlyDictionary<string, string> Values { get; init; } = values;
    public string[] AllowedMethods { get; init; } = allowedMethods;
    
}