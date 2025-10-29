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
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };
    
    public static async Task<object?> BindAsync(this HttpListenerRequest req, Type modelType, CancellationToken token =  default)
    {
        using var sr = new StreamReader(req.InputStream, req.ContentEncoding);
        var json = await sr.ReadToEndAsync(token);
        return string.IsNullOrWhiteSpace(json)
            ? Activator.CreateInstance(modelType)
            : JsonSerializer.Deserialize(json, modelType, JsonOpts);
    }

    public static async Task WriteJsonAsync(this HttpListenerResponse res, object? obj, int status = 200, CancellationToken? ct = null)
    {
        res.StatusCode = status;
        res.ContentType = "application/json; charset=utf-8";

        if (obj == null)
        {
            return;
        }
        
        var json = JsonSerializer.Serialize(obj, JsonOpts);
        var buf = Encoding.UTF8.GetBytes(json);
        
        await res.OutputStream.WriteAsync(buf, ct ?? CancellationToken.None);
    }

    public static User? GetUser(this HttpListenerRequest req)
    {
        var auth = req.Headers["Authorization"];
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ")) return null;
        var token = auth["Bearer ".Length..].Trim();
        var (ok, user) = JwtWorker.ValidateJwtToken(token);
        return ok ? user : null;
    }
}
