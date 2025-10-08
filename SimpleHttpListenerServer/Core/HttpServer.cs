using System.Collections.Concurrent;
using System.Net;
using MimeMapping;
using Npgsql;
using SimpleHttpListenerServer.Helpers;

namespace SimpleHttpListenerServer.Core;

public class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly ApiRouter _router;
    private readonly SemaphoreSlim _semaphore;
    private static readonly ConcurrentDictionary<string, string> MimeCache = new();

    public HttpServer(string prefix, NpgsqlDataSource dataSource, int maxConcurrentRequests = 50)
    {
        _listener.Prefixes.Add(prefix);
        _router = new ApiRouter(dataSource);
        _semaphore = new SemaphoreSlim(maxConcurrentRequests);
    }

    public async Task StartAsync(CancellationToken token)
    {
        _listener.Start();
        Console.WriteLine($"Server started at {string.Join(", ", _listener.Prefixes)}");

        while (_listener.IsListening && !token.IsCancellationRequested)
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                
                _ = Task.Run(async () =>
                {
                    await _semaphore.WaitAsync(token);
                    try
                    {
                        await HandleRequestAsync(ctx, token);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, token);
            }
            catch (HttpListenerException)
            {
                // бывает при Stop — игнорируем
                break;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"[SERVER ERROR] {ex}");
            }
        }

        _listener.Stop();
        _listener.Close();
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx, CancellationToken token)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            if (path.StartsWith("/api/"))
            {
                await _router.HandleAsync(ctx, token);
            }
            else
            {
                await ShowResourceFile(ctx, token);
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[REQUEST ERROR] {ex}");
            ctx.Response.StatusCode = 500;
        }
        finally
        {
            try
            {
                ctx.Response.OutputStream.Close();
                ctx.Response.Close();
            }
            catch { /* может быть ObjectDisposedException */ }
        }
    }

    private static async Task ShowResourceFile(HttpListenerContext context, CancellationToken token)
    {
        var path = context.Request.Url?.LocalPath.TrimStart('/') ?? "";
        if (string.IsNullOrEmpty(path) || path == "home") path = "index.html";

        var filePath = Path.Combine("public", path);

        if (!File.Exists(filePath))
        {
            context.Response.StatusCode = 404;
            return;
        }

        context.Response.StatusCode = 200;
        context.Response.ContentType = GetCachedMimeType(filePath);
        
        var lastModifiedUtc = File.GetLastWriteTimeUtc(filePath);
        var etag = $"{Path.GetFileName(filePath)}-{lastModifiedUtc.Ticks}";

        context.Response.AddHeader("Cache-Control", "public, max-age=3600");
        context.Response.AddHeader("Last-Modified", lastModifiedUtc.ToString("R"));
        context.Response.AddHeader("ETag", $"\"{etag}\"");

        // Проверяем — не запрашивал ли клиент ту же версию
        var clientEtag = context.Request.Headers["If-None-Match"];
        var clientModifiedSince = context.Request.Headers["If-Modified-Since"];

        if (clientEtag == $"\"{etag}\"" ||
            (DateTime.TryParse(clientModifiedSince, out var since) && lastModifiedUtc <= since))
        {
            context.Response.StatusCode = 304; // Not Modified
            return;
        }

        var bytes = await File.ReadAllBytesAsync(filePath, token);
        await context.Response.OutputStream.WriteAsync(bytes, token);
    }
    
    private static string GetCachedMimeType(string filePath)
    {
        // Проверяем кэш
        if (MimeCache.TryGetValue(filePath, out var cachedType))
            return cachedType;

        // Получаем MIME через библиотеку
        var type = MimeUtility.GetMimeMapping(filePath);

        // Добавляем в кэш
        MimeCache[filePath] = type;

        return type;
    }
}