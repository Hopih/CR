using System.Net;
using SimpleHttpListenerServer.Helpers;

var httpListener = new HttpListener();

var connectionString = "Host=localhost;Username=postgres;Password=postgres;Database=white_board";

var api = new ApiRouter(connectionString);

httpListener.Prefixes.Add("http://localhost:5000/");
httpListener.Start();
while (httpListener.IsListening)
{
    var ctx = await httpListener.GetContextAsync();
    var cts = new CancellationTokenSource();
    _ = Task.Run(async () =>
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "/";
        if (path.StartsWith("/api/"))
        {
            await api.HandleAsync(ctx, cts.Token);
        }
        else
        {
            await ShowResourceFile(ctx, cts.Token); // твоя функция раздачи статики (уже поправленная)
        }
        
        ctx.Response.OutputStream.Close();
        ctx.Response.Close();
    });
}
httpListener.Stop();
httpListener.Close();



async Task ShowResourceFile(HttpListenerContext context, CancellationToken token)
{
    if (context.Request.Url is null)
    {
        context.Response.StatusCode = 404;
        return;
    }

    // путь относительно public/
    var relativePath = context.Request.Url.LocalPath.TrimStart('/');
    if (string.IsNullOrEmpty(relativePath) || relativePath == "home")
        relativePath = "index.html"; // по умолчанию отдаём index.html
    

    var filePath = Path.Combine("public", relativePath);

    if (!File.Exists(filePath))
    {
        context.Response.StatusCode = 404;
        return;
    }

    string contentType = Path.GetExtension(filePath).ToLower() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".css"  => "text/css",
        ".js"   => "application/javascript",
        ".svg"  => "image/svg+xml",
        ".ico"  => "image/x-icon",
        ".png"  => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif"  => "image/gif",
        _       => "application/octet-stream"
    };

    context.Response.StatusCode = 200;
    context.Response.ContentType = contentType;

    var fileBytes = await File.ReadAllBytesAsync(filePath, token);
    await context.Response.OutputStream.WriteAsync(fileBytes, token);

    context.Response.Close();
}
