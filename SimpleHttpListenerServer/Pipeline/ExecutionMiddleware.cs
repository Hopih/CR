using Microsoft.Extensions.DependencyInjection;
using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Helpers;

namespace SimpleHttpListenerServer.Pipeline;

public sealed class ExecutionMiddleware(IServiceProvider sp) : BaseMiddleware
{ 
    protected override async Task InvokeCoreAsync(HttpContextEx ctx, Func<Task> next, CancellationToken ct)
    {
        var entry = ctx.Route;
        if (entry is null)
        {
            await next(); // не найден роут — отдаём статику
            return;
        }

        if (!IsAuthorized(entry.Auth, ctx))
        {
            throw new UnauthorizedAccessException();
        }

        var controller = sp.GetService(entry.ControllerType)
                         ?? sp.GetRequiredService(entry.ControllerType);

        var args = new List<object?>();

        foreach (var p in entry.Action.GetParameters())
        {
            if (p.ParameterType == typeof(HttpContextEx))
                args.Add(ctx);

            else if (p.ParameterType == typeof(CancellationToken))
                args.Add(ct);

            else if (entry.ModelParam != null && p == entry.ModelParam)
                args.Add(ctx.BoundModel);

            else if (ctx.RouteValues?.TryGetValue(p.Name!, out var raw) == true)
                args.Add(ConvertSimple(raw, p.ParameterType));

            else
                args.Add(Type.Missing);
        }

        var result = entry.Action.Invoke(controller, args.ToArray());

        if (result is Task task)
        {
            await task.ConfigureAwait(false);
            var type = task.GetType();
            if (typeof(Task).IsAssignableFrom(type) && type.IsGenericType)
            {
                var val = type.GetProperty("Result")?.GetValue(task);
                if (val is not null)
                {
                    await ctx.Response.WriteJsonAsync(val, ct: ct);
                }
            }
        }
        else if (result is not null)
        {
            await ctx.Response.WriteJsonAsync(result, ct: ct);
        }
        else if (result is null && ctx.Response is { StatusCode: >= 400, OutputStream.CanWrite: true })
        {
            // если контроллер выставил ошибку, но не вернул тело
            await ctx.Response.WriteJsonAsync(new
            {
                error = "RequestFailed",
                code = ctx.Response.StatusCode
            });
        }
    }

    private static bool IsAuthorized(AuthorizeAttribute? auth, HttpContextEx ctx)
    {
        if (auth is null || auth.AllowAnonymous || !auth.Required.HasFlag(AuthTokenKind.Jwt))
        {
            return true;
        }

        if (auth.Required.HasFlag(AuthTokenKind.Guest) && string.IsNullOrEmpty(ctx.GuestToken) ||
            ctx.AuthUser is null)
        {
            return false;
        }

        return auth.Role is null ||
               string.Equals(ctx.AuthUser?.Role, auth.Role, StringComparison.OrdinalIgnoreCase);
    }

    private static object? ConvertSimple(string value, Type type)
    {
        if (type == typeof(string))
        {
            return value;
        }

        if (type == typeof(int) && int.TryParse(value, out var integer))
        {
            return integer;
        }
        
        if (type == typeof(long) && long.TryParse(value, out var l))
        {
            return l;
        }

        if (type == typeof(Guid) && Guid.TryParse(value, out var guid))
        {
            return guid;
        }
        
        if (type == typeof(bool) && bool.TryParse(value, out var b))
        {
            return b;
        }

        return Convert.ChangeType(value, type);
    }
    
    private static bool HttpMethodAllowsBody(string method)
    {
        return method.Equals("POST", StringComparison.OrdinalIgnoreCase)
               || method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
               || method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);
    }
}