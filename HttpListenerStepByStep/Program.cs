using System.Net;
using System.Text;
using System.Text.Json;
using CodeJam;
using HttpListenerStepByStep;
using HttpListenerStepByStep.Data;
using HttpListenerStepByStep.Http;

using var httpListener = new HttpListener();

var connectionString = Environment.GetEnvironmentVariable("BOARD_DB_CONNECTION")
    ?? "Host=localhost;Port=5432;Database=my_miro;Username=postgres;Password=postgres";
var boardRepository = new BoardRepository(connectionString);
var stickerRepository = new StickerRepository(connectionString);
var boardApi = new BoardApiHandler(boardRepository, stickerRepository);
var quotesClient = new QuotesApiClient();

httpListener.Prefixes.Add("http://localhost:5000/");
httpListener.Start();


while (httpListener.IsListening)
{
    var context = await httpListener.GetContextAsync();
    _ = Task.Run(async () =>
    {
        try
        {
            var path = context.Request.Url?.AbsolutePath ?? "/";
            if (path == "/api" || path.StartsWith("/api/", StringComparison.Ordinal))
            {
                await boardApi.HandleAsync(context, CancellationToken.None);
            }
            
            else if (path == "/quote/random")
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
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToDiagnosticString());
            // Ошибка записи клиенту тоже возможна, если он закрыл соединение.
            try
            {
                await ApiSupport.WriteAsync(context.Response, 500,
                    new { error = new { code = "server_error", message = "Ошибка сервера" } });
            }
            catch (Exception writeError)
            {
                Console.Error.WriteLine(writeError.Message);
            }
        }
        finally
        {
            context.Response.Close();
        }
        
    });
}

httpListener.Stop();
httpListener.Close();
quotesClient.Dispose();
return;

async Task ShowStatic(HttpListenerContext context, CancellationToken ctx = default)
{
    var relative = context.Request.Url?.LocalPath.TrimStart('/') ?? "";
    if (relative is "" or "index")
    {
        relative = "index.html";
    }

    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "public"));
    var target = Path.GetFullPath(Path.Combine(root, relative));
    var rootPrefix = root + Path.DirectorySeparatorChar;
    
    var comparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase 
        : StringComparison.Ordinal;
    
    if (!target.StartsWith(rootPrefix, comparison) || !File.Exists(target))
    {
        await Show404(context, ctx);
        return;
    }
    await ShowFile(target, context, ctx);
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

