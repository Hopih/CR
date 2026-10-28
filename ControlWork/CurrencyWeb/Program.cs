using System.Net;
using CurrencyWeb;

Console.OutputEncoding = System.Text.Encoding.UTF8;
using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
var ratesService = new RatesService(httpClient);
using var listener = new HttpListener();
listener.Prefixes.Add("http://localhost:8081/");
var server = new WebServer(listener, ratesService);

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    listener.Stop();
};

try
{
    listener.Start();
    Console.WriteLine("Курсы валют: http://localhost:8081/ (Ctrl+C — выход)");
    await server.RunAsync();
}
catch (HttpListenerException ex)
{
    Console.Error.WriteLine($"Не удалось запустить или продолжить работу HTTP-сервера: {ex.Message}");
    Environment.ExitCode = 1;
}
