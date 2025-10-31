using Microsoft.Extensions.DependencyInjection;

namespace SimpleHttpListenerServer.Core;

public sealed class MiddlewarePipeline
{
    private readonly List<Func<IServiceProvider, IMiddleware>> _middlewareFactories = new();

    public MiddlewarePipeline Use(Func<HttpContextEx, Func<Task>, Task> inline)
    {
        _middlewareFactories.Add(_ => new DelegateMiddleware(inline));
        return this;
    }
    
    public MiddlewarePipeline Use<TMiddleware>() where TMiddleware : IMiddleware
    {
        _middlewareFactories.Add(sp => sp.GetRequiredService<TMiddleware>());
        return this;
    }

    public async Task ExecuteAsync(HttpContextEx ctx, IServiceProvider sp)
    {
        // создаём экземпляры из текущего scope
        var middlewares = _middlewareFactories.Select(f => f(sp)).ToArray();

        // формируем цепочку вызовов
        var next = () => Task.CompletedTask;

        for (var i = middlewares.Length - 1; i >= 0; i--)
        {
            var current = middlewares[i];
            var nextCopy = next;
            next = () => current.InvokeAsync(ctx, nextCopy);
        }

        await next();
    }
}

/// Вспомогательный middleware для делегатов
internal sealed class DelegateMiddleware(Func<HttpContextEx, Func<Task>, Task> handler) : BaseMiddleware
{
    protected override async Task InvokeCoreAsync(HttpContextEx ctx, Func<Task> next, CancellationToken ct)
    {
        await handler(ctx, next);
    }
}