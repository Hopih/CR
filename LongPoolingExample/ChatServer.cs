using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace LongPoolingExample;

public class ChatServer
{
    private readonly HttpListener _listener = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<HttpListenerContext>> _waiting = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _history = new();

    public async Task StartAsync(string prefix, CancellationToken ct = default)
    {
        _listener.Prefixes.Add(prefix);
        _listener.Start();
        Console.WriteLine($"Listening on {prefix}");

        while (!ct.IsCancellationRequested)
        {
            var ctx = await _listener.GetContextAsync();
            _ = Task.Run(() => HandleRequestAsync(ctx), ct);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url!.AbsolutePath;
        switch (path)
        {
            case "/":
                await ServeHtml(ctx);
                break;
            case "/poll":
                await HandlePoll(ctx);
                break;
            case "/send":
                await HandleSend(ctx);
                break;
            case "/rooms":
                await HandleRooms(ctx);
                break;
            default:
                ctx.Response.StatusCode = 404;
                ctx.Response.Close();
                break;
        }
    }

    private async Task HandleRooms(HttpListenerContext ctx)
    {
        var rooms = _history.Keys;
        await SendJsonAsync(ctx, new { rooms });
    }

    private async Task HandlePoll(HttpListenerContext ctx)
    {
        var room = ctx.Request.QueryString["room"] ?? "default";

        if (_history.TryGetValue(room, out var queue) && queue.TryDequeue(out var msg))
        {
            await SendJsonAsync(ctx, new { messages = new[] { msg } });
            return;
        }

        var bag = _waiting.GetOrAdd(room, _ => new ConcurrentBag<HttpListenerContext>());
        bag.Add(ctx);
        Console.WriteLine($"Client waiting in room: {room}");

        _ = Task.Delay(30000).ContinueWith(async _ =>
        {
            if (ctx.Response.OutputStream.CanWrite)
                await SendJsonAsync(ctx, new { messages = Array.Empty<string>() });
        });
    }

    private async Task HandleSend(HttpListenerContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var msg = JsonSerializer.Deserialize<ChatMessage>(body, options);
        
        if (msg == null || string.IsNullOrWhiteSpace(msg.Text))
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.Close();
            return;
        }

        var room = msg.Room ?? "default";
        var formatted = $"{msg.User}: {msg.Text}";
        Console.WriteLine($"[{room}] {formatted}");

        var queue = _history.GetOrAdd(room, _ => new ConcurrentQueue<string>());
        queue.Enqueue(formatted);
        while (queue.Count > 20 && queue.TryDequeue(out _)) { }

        if (_waiting.TryRemove(room, out var clients))
        {
            foreach (var wctx in clients)
                await SendJsonAsync(wctx, new { messages = new[] { formatted } });
        }

        ctx.Response.StatusCode = 200;
        ctx.Response.Close();
    }

    private async Task ServeHtml(HttpListenerContext ctx)
    {
        var html = await File.ReadAllTextAsync("chat.html");
        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(html));
        ctx.Response.Close();
    }

    private static async Task SendJsonAsync(HttpListenerContext ctx, object obj)
    {
        try
        {
            var json = JsonSerializer.Serialize(obj);
            var bytes = Encoding.UTF8.GetBytes(json);
            ctx.Response.ContentType = "application/json";
            await ctx.Response.OutputStream.WriteAsync(bytes);
            ctx.Response.Close();
        }
        catch
        {
            // ignored
        }
    }

    private record ChatMessage(string? Room, string User, string Text);
}