using System.Net;
using System.Text;
using System.Text.Json;
using HttpListenerStepByStep;

using var httpListener = new HttpListener();
httpListener.Prefixes.Add("http://localhost:5000/");
httpListener.Start();
var quotesClient = new QuotesApiClient();

while (httpListener.IsListening)
{
    var context = await httpListener.GetContextAsync();
    _ = Task.Run(async () =>
    {
        if (context.Request.Url?.LocalPath == "/quote/random")
        {
            var result = await quotesClient.GetRandomQuoteAsync(CancellationToken.None);
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result, new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })));

        }
        else
        {
            await ShowStatic(context);
        }
        context.Response.OutputStream.Close();
        context.Response.Close();
    });
}

httpListener.Stop();
httpListener.Close();
quotesClient.Dispose();
return;

async Task ShowStatic(HttpListenerContext context, CancellationToken ctx = default)
{
    var localPath = context.Request.Url?.LocalPath.TrimStart('/') ?? "";
    if (string.IsNullOrEmpty(localPath) || localPath == "index")
        localPath = "index.html";

    var filePath = Path.Combine("public", localPath);
    if (File.Exists(filePath))
    {
        await ShowFile(filePath, context, ctx);
    }
    else
    {
        await Show404(context, ctx);
    }
}
    
async Task Show404(HttpListenerContext context, CancellationToken ctx)
{
    context.Response.ContentType = "text/plain; charset=utf-8";
    context.Response.StatusCode = 404;
    await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Нужная вам страница не найдена!"), ctx);
}

async Task ShowFile(string path, HttpListenerContext context, CancellationToken ctx)
{
    context.Response.StatusCode = 200;
    context.Response.ContentType = Path.GetExtension(path) switch
    {
        ".js" => "application/javascript; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".html" => "text/html; charset=utf-8",
        ".svg" => "image/svg+xml; charset=utf-8",
        _ => "text/plain; charset=utf-8"
    };
    var file = await File.ReadAllBytesAsync(path, ctx);
    await context.Response.OutputStream.WriteAsync(file, ctx);
}

