using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
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
                var search = context.Request.QueryString["search"]?.Trim();
                if (search?.Length > 50)
                {
                    await WriteJsonAsync(response, 400, new { error = "Поиск — не длиннее 50 символов." });
                    return;
                }
                
                // null, если параметра нет; Trim — пробелы по краям не считаются.
                var query = context.Request.QueryString;
                var sort = query["sort"]?.Trim() ?? "code";
                var order = query["order"]?.Trim() ?? "asc";
                
                var lst = ratesService.GetRatesAsync().Result.Currencies;
                
                var sorted = sort switch
                {
                    "name" => lst.OrderBy(c => c.Name),
                    "rate" => lst.OrderBy(c => c.Value/c.Nominal),
                    _      => lst.OrderBy(c => c.CharCode),
                };
                var result = (order == "desc" ? sorted.Reverse() : sorted).ToArray();
                await WriteJsonAsync(response, 200, new RatesResponse(ratesService.GetRatesAsync().Result.Date, result));
            }
            else if (StaticFiles.TryGetValue(path, out var file))
            {
                var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "public", file.File));
                response.ContentType = file.ContentType;
                response.ContentLength64 = bytes.Length;
                await response.OutputStream.WriteAsync(bytes);
            }

            else if (path == "/api/convert")
            {
                var query = context.Request.QueryString;

                // query["from"] вернёт null, если параметра нет (словарь бросил бы исключение → 500).
                // ToUpperInvariant — чтобы usd и USD были одним и тем же.
                var fromCode = query["from"]?.Trim().ToUpperInvariant() ?? "";
                var toCode = query["to"]?.Trim().ToUpperInvariant() ?? "";
                if (fromCode.Length != 3 || toCode.Length != 3 || !fromCode.All(char.IsAsciiLetter) || !toCode.All(char.IsAsciiLetter))
                {
                    await WriteJsonAsync(response, 400, new { error = "Код валюты — три латинские буквы, например USD." });
                    return;
                }

                // TryParse вместо Parse: на "abc" вернёт false, а не упадёт. "1,5" и "1.5" — оба варианта,
                // InvariantCulture — точка как разделитель даже на русской Windows.
                var amountText = query["amount"]?.Trim().Replace(',', '.');
                if (!decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                    || amount <= 0 || amount > 1_000_000_000)
                {
                    await WriteJsonAsync(response, 400, new { error = "Сумма — число больше 0 и не больше 1 000 000 000." });
                    return;
                }

                var rates = await ratesService.GetRatesAsync();

                // Сколько рублей стоит 1 штука каждой валюты (курс дан за номинал). Рубля в списке нет — для него 1.
                var rubPerUnit = rates.Currencies.ToDictionary(c => c.CharCode, c => c.Value / c.Nominal);
                rubPerUnit["RUB"] = 1;
                if (!rubPerUnit.TryGetValue(fromCode, out var fromRub) || !rubPerUnit.TryGetValue(toCode, out var toRub))
                {
                    await WriteJsonAsync(response, 404, new { error = $"Валюты {fromCode} или {toCode} нет в курсах." });
                    return;
                }

                var rate = fromRub / toRub;   // без Round
                var result = rate * amount;   // без Round

                await WriteJsonAsync(response, 200, new { from = fromCode, to = toCode, amount,
                    result = Math.Round(result, 2), rate = Math.Round(rate, 4), date = rates.Date });   // Round ТОЛЬКО здесь
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
