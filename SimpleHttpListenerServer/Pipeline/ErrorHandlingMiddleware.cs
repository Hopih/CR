using SimpleHttpListenerServer.Core;

namespace SimpleHttpListenerServer.Pipeline;

public class ErrorHandlingMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContextEx ctx, Func<Task> next)
    {
        try { await next(); }
        catch (ValidationException vex)
        {
            await ctx.WriteJsonAsync(new { error = "ValidationFailed", details = vex.Errors }, 400);
        }
        catch (UnauthorizedAccessException)
        {
            await ctx.WriteJsonAsync(new { error = "Unauthorized" }, 401);
        }
        catch (Exception ex)
        {
            // логирование сюда
            await ctx.WriteJsonAsync(new { error = "InternalServerError", trace = ex.Message }, 500);
        }
    }
}

// Простейшая валидация под fluent-стиль:
public interface IValidator<T>
{
    (bool IsValid, IEnumerable<string> Errors) Validate(T model, HttpContextEx ctx);
}
public sealed class ValidationException(IEnumerable<string> errors) : Exception("Validation failed")
{
    public IEnumerable<string> Errors { get; } = errors;
}