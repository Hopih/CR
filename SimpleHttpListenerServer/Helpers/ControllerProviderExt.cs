using Microsoft.Extensions.DependencyInjection;
using SimpleHttpListenerServer.Core;
using ServiceCollection = SimpleHttpListenerServer.Core.ServiceCollection;

namespace SimpleHttpListenerServer.Helpers;

public static class ControllerProviderExt
{
    public static IServiceProvider UseControllers(this IServiceProvider sp)
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
    
    public static ServiceCollection AddControllers(this ServiceCollection services)
    {
        // можно искать контроллеры автоматически
        var controllers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Controller"))
            .ToArray();

        foreach (var controller in controllers)
        {
            services.AddSingleton(controller);
        }
        
        return services;
    }
}