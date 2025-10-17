using Microsoft.Extensions.DependencyInjection;
using SimpleHttpListenerServer.Core;

namespace SimpleHttpListenerServer.Helpers;

public static class ControllerProviderExt
{
    public static IServiceProvider AddControllers(this IServiceProvider sp)
    {
        var routeTable = sp.GetRequiredService<RouteTable>();

        // можно искать контроллеры автоматически
        var controllers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Controller"))
            .ToArray();
        
        routeTable.BuildFromControllers(controllers);
        
        return sp;
    }
}