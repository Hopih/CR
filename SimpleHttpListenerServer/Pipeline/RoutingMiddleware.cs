using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Helpers;

namespace SimpleHttpListenerServer.Pipeline;

public sealed class RoutingMiddleware(RouteTable routes) : BaseMiddleware
{
    protected override async Task InvokeCoreAsync(HttpContextEx ctx, Func<Task> next, CancellationToken ct)
    {
        var match = routes.Match(ctx.Request.Url!.AbsolutePath, ctx.Request.HttpMethod);
        if (match is null)
        {
            await next(); // не найден — передаём в StaticFileMiddleware
            return;
        }

        var (entry, routeValues) = match.Value;

        ctx.Route = entry; // сохраняем найденный роут в контекст

        // биндим модель, если есть
        if (entry.ModelParam is { } mp && ctx.Request.HasEntityBody)
        {
            var model = await ctx.Request.BindAsync(mp.ParameterType, token: ct);
            ctx.BoundModel = model;
        }

        ctx.RouteValues = routeValues;
        
        await next();
    }
}