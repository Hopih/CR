using Microsoft.Extensions.DependencyInjection;

namespace SimpleHttpListenerServer.Core;

public abstract class BaseMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContextEx ctx, Func<Task> next)
    {
        // если клиент закрыл соединение — прерываем выполнение
        if (ctx.RequestAborted.IsCancellationRequested)
        {
            return;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await InvokeCoreAsync(ctx, next, ctx.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[{GetType().Name}] Canceled after {sw.ElapsedMilliseconds}ms");
        }
        finally
        {
            sw.Stop();
        }
    }

    /// <summary>
    /// Реализуется в наследниках; сюда автоматически передаётся CancellationToken.
    /// </summary>
    protected abstract Task InvokeCoreAsync(HttpContextEx ctx, Func<Task> next, CancellationToken ct);
}