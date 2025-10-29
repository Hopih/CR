namespace SimpleHttpListenerServer.Core;

public interface IMiddleware
{
    Task InvokeAsync(HttpContextEx ctx, Func<Task> next);
}