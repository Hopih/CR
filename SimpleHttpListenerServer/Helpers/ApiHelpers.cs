using System.Net;
using SimpleHttpListenerServer.Entities;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleHttpListenerServer.Helpers;

public static class ApiHelpers
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static async Task<T?> ReadJsonAsync<T>(this HttpListenerRequest req, CancellationToken ct)
    {
        using var sr = new StreamReader(req.InputStream, req.ContentEncoding);
        var body = await sr.ReadToEndAsync(ct);
        return string.IsNullOrWhiteSpace(body) 
            ? default 
            : JsonSerializer.Deserialize<T>(body, JsonOpts);
    }
    
    public static async Task<object?> BindAsync(this HttpListenerRequest req, Type modelType)
    {
        using var sr = new StreamReader(req.InputStream, req.ContentEncoding);
        var json = await sr.ReadToEndAsync();
        return string.IsNullOrWhiteSpace(json)
            ? Activator.CreateInstance(modelType)
            : JsonSerializer.Deserialize(json, modelType);
    }

    public static async Task WriteJsonAsync(this HttpListenerResponse res, object obj, int status = 200, CancellationToken? ct = null)
    {
        var json = JsonSerializer.Serialize(obj, JsonOpts);
        var buf = Encoding.UTF8.GetBytes(json);
        res.StatusCode = status;
        res.ContentType = "application/json; charset=utf-8";
        await res.OutputStream.WriteAsync(buf, ct ?? CancellationToken.None);
        res.Close();
    }

    public static async Task WriteErrorAsync(this HttpListenerResponse res, int status, string message, CancellationToken ct)
    {
        await WriteJsonAsync(res, 
            new
            {
                error = message
            }, 
            status, ct);
    }

    public static User? GetUser(this HttpListenerRequest req)
    {
        var auth = req.Headers["Authorization"];
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ")) return null;
        var token = auth["Bearer ".Length..].Trim();
        var (ok, user) = JwtWorker.ValidateJwtToken(token);
        return ok ? user : null;
    }

    public static string? GetCookie(this HttpListenerRequest req, string name)
    {
        return req.Cookies[name]?.Value;
    }
}
