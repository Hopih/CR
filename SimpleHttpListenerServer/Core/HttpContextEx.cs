using System.Net;
using SimpleHttpListenerServer.Entities;

namespace SimpleHttpListenerServer.Core;

using System.Text.Json;

public sealed class HttpContextEx
{
    public HttpContextEx(HttpListenerContext raw, IServiceProvider sp)
    {
        Raw = raw;
        Services = sp;
        var cts = new CancellationTokenSource();
        Raw.Response.OutputStream
            .BeginWrite([], 0, 0, _ =>
            {
                if (!Raw.Response.OutputStream.CanWrite)
                    cts.Cancel();
            }, null);

        RequestAborted = cts.Token;
    }

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
    
    public RouteEntry? Route { get; set; }
    public Dictionary<string, string>? RouteValues { get; set; }
    
    public CancellationToken RequestAborted { get; }
}