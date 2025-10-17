using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Helpers;

namespace SimpleHttpListenerServer.Pipeline;

public sealed class RoutingMiddleware(RouteTable routes, ServiceProvider sp) : IMiddleware
{
    public async Task InvokeAsync(HttpContextEx ctx, Func<Task> next)
    {
        var match = routes.Match(ctx.Request.Url!.AbsolutePath, ctx.Request.HttpMethod);
        if (match is null)
        {
            await next();
            return;
        }

        var (entry, routeValues) = match.Value;

        // 1) Авторизация
        if (!IsAuthorized(entry.Auth, ctx))
        {
            await ctx.WriteJsonAsync(new { error = "Unauthorized" }, 401);
            return;
        }

        // 2) Контроллер
        var controller = sp.GetService(entry.ControllerType) 
                      ?? sp.GetRequiredService(entry.ControllerType);

        // 3) Аргументы метода
        var paramVals = new List<object?>();

        foreach (var p in entry.Action.GetParameters())
        {
            if (p.ParameterType == typeof(HttpContextEx))
            {
                paramVals.Add(ctx);
                continue;
            }

            if (entry.ModelParam != null && p == entry.ModelParam)
            {
                if (ctx.Request.ContentLength64 > 0)
                {
                    var model = await ctx.Request.BindAsync(p.ParameterType);
                    ctx.BoundModel = model;
                    paramVals.Add(model);
                }
                else
                {
                    paramVals.Add(Activator.CreateInstance(p.ParameterType));
                }
                continue;
            }

            if (routeValues.TryGetValue(p.Name!, out var raw))
            {
                paramVals.Add(ConvertSimple(raw, p.ParameterType));
            }
            else
            {
                paramVals.Add(p.HasDefaultValue ? p.DefaultValue : GetDefault(p.ParameterType));
            }
        }

        // 4) Вызов действия
        var result = entry.Action.Invoke(controller, paramVals.ToArray());

        if (result is Task task)
        {
            await task.ConfigureAwait(false);

            var type = task.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var value = type.GetProperty("Result")?.GetValue(task);
                if (value is not null)
                    await ctx.WriteJsonAsync(value);
            }
        }
        else if (result is not null)
        {
            await ctx.WriteJsonAsync(result);
        }
    }

    private static object? GetDefault(Type type)
        => type.IsValueType ? Activator.CreateInstance(type) : null;

    private static object? ConvertSimple(string value, Type type)
    {
        if (type == typeof(string)) return value;
        if (type == typeof(int) && int.TryParse(value, out var i)) return i;
        if (type == typeof(long) && long.TryParse(value, out var l)) return l;
        if (type == typeof(Guid) && Guid.TryParse(value, out var g)) return g;
        if (type.IsEnum && Enum.TryParse(type, value, true, out var e)) return e;
        return value;
    }

    private static bool IsAuthorized(AuthorizeAttribute? auth, HttpContextEx ctx)
    {
        if (auth is null || auth.AllowAnonymous) return true;

        if (auth.Required.HasFlag(AuthTokenKind.Guest) && string.IsNullOrEmpty(ctx.GuestToken))
            return false;

        if (auth.Required.HasFlag(AuthTokenKind.Jwt))
        {
            if (string.IsNullOrEmpty(ctx.JwtToken) || ctx.AuthUser is null) return false;
            if (auth.Role is not null &&
                !string.Equals(ctx.AuthUser?.Role, auth.Role, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}