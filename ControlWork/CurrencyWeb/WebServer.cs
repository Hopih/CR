using System.Net;
using System.Text.Json;

namespace CurrencyWeb;

public sealed class WebServer(HttpListener listener, RatesService ratesService)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Dictionary<string, (string File, string ContentType)> StaticFiles = new()
    {
        ["/"] = ("index.html", "text/html; charset=utf-8"),
        ["/index.html"] = ("index.html", "text/html; charset=utf-8"),
        ["/app.js"] = ("app.js", "text/javascript; charset=utf-8"),
        ["/styles.css"] = ("styles.css", "text/css; charset=utf-8")
    };

    public async Task RunAsync()
    {
        while (listener.IsListening)
        {
            HttpListenerContext context;
            try { context = await listener.GetContextAsync(); }
            catch (HttpListenerException) when (!listener.IsListening) { break; }
            catch (ObjectDisposedException) when (!listener.IsListening) { break; }
            _ = HandleAsync(context);
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        var response = context.Response;
        try
        {
            if (context.Request.HttpMethod != "GET")
            {
                response.Headers["Allow"] = "GET";
                await WriteJsonAsync(response, 405, new { error = "Поддерживается только GET." });
                return;
            }

            var path = context.Request.Url!.AbsolutePath;
            if (path == "/api/rates")
            {
                await WriteJsonAsync(response, 200, await ratesService.GetRatesAsync());
            }
            else if (StaticFiles.TryGetValue(path, out var file))
            {
                var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "public", file.File));
                response.ContentType = file.ContentType;
                response.ContentLength64 = bytes.Length;
                await response.OutputStream.WriteAsync(bytes);
            }
            else
            {
                await WriteJsonAsync(response, 404, new { error = "Маршрут не найден." });
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);
            try
            {
                var upstreamError = ex is HttpRequestException or TaskCanceledException or JsonException;
                await WriteJsonAsync(response, upstreamError ? 502 : 500,
                    new { error = upstreamError ? "Не удалось получить курсы. Повторите попытку позже." : "Ошибка сервера." });
            }
            catch (Exception writeError) when (writeError is HttpListenerException or IOException or ObjectDisposedException)
            {
                await Console.Error.WriteLineAsync("Клиент закрыл соединение.");
            }
        }
        finally { response.Close(); }
    }

    private static async Task WriteJsonAsync(HttpListenerResponse response, int status, object body)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(body, JsonOptions);
        response.StatusCode = status;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
    }
}
