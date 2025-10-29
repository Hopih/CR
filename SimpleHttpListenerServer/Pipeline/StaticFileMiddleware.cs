using System.Collections.Concurrent;
using MimeMapping;
using SimpleHttpListenerServer.Core;

namespace SimpleHttpListenerServer.Pipeline;

public sealed class StaticFileMiddleware(string root) : BaseMiddleware
{
    private static readonly ConcurrentDictionary<string, string> MimeCache = new();

    protected override async Task InvokeCoreAsync(HttpContextEx ctx, Func<Task> next, CancellationToken ct)
    {
        var request = ctx.Request;
        var response = ctx.Response;

        var localPath = request.Url?.LocalPath.TrimStart('/') ?? "";
        if (string.IsNullOrEmpty(localPath) || localPath == "home")
            localPath = "index.html";

        var filePath = Path.Combine(root, localPath);
        if (!File.Exists(filePath))
        {
            await next(); // если нет файла, передаем управление дальше (404 обработает ErrorMiddleware)
            return;
        }

        response.ContentType = GetCachedMimeType(filePath);
        response.StatusCode = 200;

        var lastModifiedUtc = File.GetLastWriteTimeUtc(filePath);
        var etag = $"{Path.GetFileName(filePath)}-{lastModifiedUtc.Ticks}";

        response.AddHeader("Cache-Control", "public, max-age=3600");
        response.AddHeader("Last-Modified", lastModifiedUtc.ToString("R"));
        response.AddHeader("ETag", $"\"{etag}\"");

        // Проверка на кэш у клиента
        var clientEtag = request.Headers["If-None-Match"];
        var clientModifiedSince = request.Headers["If-Modified-Since"];

        if (clientEtag == $"\"{etag}\"" ||
            (DateTime.TryParse(clientModifiedSince, out var since) && lastModifiedUtc <= since))
        {
            response.StatusCode = 304;
            return;
        }

        await using var fs = File.OpenRead(filePath);
        await fs.CopyToAsync(response.OutputStream, ct);
    }

    private static string GetCachedMimeType(string filePath)
    {
        if (MimeCache.TryGetValue(filePath, out var cachedType))
            return cachedType;

        var type = MimeUtility.GetMimeMapping(filePath);
        MimeCache[filePath] = type;
        return type;
    }
}