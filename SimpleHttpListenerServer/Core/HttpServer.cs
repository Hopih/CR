using System.Net;

namespace SimpleHttpListenerServer.Core;

public class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly SemaphoreSlim _semaphore;
    private readonly MiddlewarePipeline _middlewarePipeline;
    private readonly IServiceProvider _services;

    public HttpServer(string prefix, MiddlewarePipeline pipeline, IServiceProvider services, int maxConcurrentRequests = 50)
    {
        _listener.Prefixes.Add(prefix);
        _middlewarePipeline = pipeline;
        _services = services;
        _semaphore = new SemaphoreSlim(maxConcurrentRequests);
    }

    public async Task StartAsync(CancellationTokenSource cts)
    {
        _listener.Start();
        Console.WriteLine($"Server started at {string.Join(", ", _listener.Prefixes)}");

        while (_listener.IsListening && !cts.Token.IsCancellationRequested)
        {
            var ctx = await _listener.GetContextAsync();
            try
            {
                _ = Task.Run(async () =>
                {
                    await _semaphore.WaitAsync(cts.Token);
                    try
                    {
                        await _middlewarePipeline.ExecuteAsync(new HttpContextEx(ctx, _services, cts));
                    }
                    finally
                    {
                        _semaphore.Release();
                        ctx.Response.OutputStream.Close();
                        ctx.Response.Close();
                    }
                }, cts.Token);
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
}