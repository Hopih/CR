using System.Net;
using SimpleHttpListenerServer.Entities;

namespace SimpleHttpListenerServer.Core;

using System.Text.Json;

public sealed class HttpContextEx
{
    public HttpListenerContext Raw { get; }
    public IServiceProvider Services { get; }

    public HttpListenerRequest Request => Raw.Request;
    public HttpListenerResponse Response => Raw.Response;
    
    public string? GuestToken { get; set; }
    public string? JwtToken { get; set; }
    public User? AuthUser { get; set; }
    public bool IsAuthenticated { get; set; }
    
    public object? BoundModel { get; set; }
    public Dictionary<string, object> Items { get; } = new();

    public HttpContextEx(HttpListenerContext raw, IServiceProvider sp) { Raw = raw; Services = sp; }

    public async Task WriteJsonAsync(object value, int status = 200)
    {
        Response.StatusCode = status;
        Response.ContentType = "application/json; charset=utf-8";
        await using var sw = new StreamWriter(Response.OutputStream);
        await sw.WriteAsync(JsonSerializer.Serialize(value));
    }
}

public sealed record JwtPayload(string? Sub, string? Role, DateTime? Exp);

// простейший JSON-биндер (1 параметр модели на метод)
public static class JsonBodyBinder
{
    public static async Task<object?> BindAsync(HttpListenerRequest req, Type modelType)
    {
        using var sr = new StreamReader(req.InputStream, req.ContentEncoding);
        var json = await sr.ReadToEndAsync();
        return string.IsNullOrWhiteSpace(json)
            ? Activator.CreateInstance(modelType)
            : JsonSerializer.Deserialize(json, modelType);
    }
}