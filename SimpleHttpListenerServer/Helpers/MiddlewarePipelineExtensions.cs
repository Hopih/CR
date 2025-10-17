
using Microsoft.Extensions.DependencyInjection;
using SimpleHttpListenerServer.Core;

namespace SimpleHttpListenerServer.Helpers;

public static class MiddlewarePipelineExtensions
{
    public static MiddlewarePipeline Use<T>(this MiddlewarePipeline pipeline, ServiceProvider sp)
        where T : IMiddleware
    {
        var m = (IMiddleware)sp.GetRequiredService(typeof(T));
        return pipeline.Use(m.InvokeAsync);
    }
}