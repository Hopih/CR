using System.Net;

namespace HttpListenerStepByStep.Http;

public sealed class StaticFileHandler(string directory)
{
    public async Task HandleAsync(HttpListenerContext context, CancellationToken ct)
    {
        if (context.Request.HttpMethod is not ("GET" or "HEAD"))
        {
            context.Response.Headers["Allow"] = "GET, HEAD";
            throw new ApiException(405, "method_not_allowed", "Метод не разрешён");
        }
        var name = context.Request.Url!.LocalPath.TrimStart('/');
        if (name is "" or "index") name = "index.html";
        var root = Path.GetFullPath(directory);
        var path = Path.GetFullPath(Path.Combine(root, name));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, comparison) ||
            !File.Exists(path))
            throw new ApiException(404, "not_found", "Файл не найден");
        context.Response.ContentType = Path.GetExtension(path) switch
        {
            ".js" => "application/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".html" => "text/html; charset=utf-8",
            ".svg" => "image/svg+xml", _ => "application/octet-stream"
        };
        var bytes = await File.ReadAllBytesAsync(path, ct);
        context.Response.ContentLength64 = bytes.Length;
        if (context.Request.HttpMethod != "HEAD")
            await context.Response.OutputStream.WriteAsync(bytes, ct);
    }
}