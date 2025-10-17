namespace SimpleHttpListenerServer.Core;

public sealed class MiddlewarePipeline
{
    private readonly List<Func<HttpContextEx, Func<Task>, Task>> _stack = [];

    public MiddlewarePipeline Use(Func<HttpContextEx, Func<Task>, Task> inline)
    {
        _stack.Add(inline); 
        return this;
    }

    public async Task ExecuteAsync(HttpContextEx ctx)
    {
        var i = -1;
        await Next();
        return;

        Task Next()
        {
            i++; 
            return i < _stack.Count 
                ? _stack[i](ctx, Next) 
                : Task.CompletedTask;
        }
    }
}