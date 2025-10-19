using SimpleHttpListenerServer.Core;
using SimpleHttpListenerServer.Helpers;

namespace SimpleHttpListenerServer.Pipeline;

public class ErrorHandlingMiddleware : BaseMiddleware
{
    protected override async Task InvokeCoreAsync(HttpContextEx ctx, Func<Task> next, CancellationToken ct)
    {
        try
        {
            await next();
        }
        catch (ValidationException vex)
        {
            await ctx.Response.WriteJsonAsync(new { error = "ValidationFailed", details = vex.Errors }, 400);
        }
        catch (UnauthorizedAccessException)
        {
            await ctx.Response.WriteJsonAsync(new { error = "Unauthorized" }, 401);
        }
        catch (Exception ex)
        {
            // логирование сюда
            await ctx.Response.WriteJsonAsync(new { error = "InternalServerError", trace = ex.Message }, 500);
        }
    }
}

public sealed class ValidationException(IEnumerable<string> errors) : Exception("Validation failed")
{
    public IEnumerable<string> Errors { get; } = errors;
}