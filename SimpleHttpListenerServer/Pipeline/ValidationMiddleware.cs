using SimpleHttpListenerServer.Core;

namespace SimpleHttpListenerServer.Pipeline;

using FluentValidation.Results;

public sealed class ValidationMiddleware(IServiceProvider sp) : IMiddleware
{
    public async Task InvokeAsync(HttpContextEx ctx, Func<Task> next)
    {
        // если модель уже привязана (например, роутером или binder'ом)
        var model = ctx.BoundModel;
        if (model is not null)
        {
            // ищем IValidator<T> для типа модели
            var validatorType = typeof(IValidator<>).MakeGenericType(model.GetType());
            var validator = sp.GetService(validatorType);

            if (validator is not null)
            {
                // вызываем ValidateAsync(T instance)
                var method = validatorType.GetMethod("ValidateAsync", [model.GetType(), typeof(CancellationToken)])
                             ?? validatorType.GetMethod("Validate", [model.GetType()]);

                if (method is not null)
                {
                    object? resultObj;

                    if (method.Name == "ValidateAsync")
                        resultObj = await (dynamic)method.Invoke(validator, [model, CancellationToken.None])!;
                    else
                        resultObj = method.Invoke(validator, [model]);

                    if (resultObj is ValidationResult { IsValid: false } result)
                    {
                        ctx.Response.StatusCode = 400;
                        await ctx.WriteJsonAsync(new
                        {
                            error = "ValidationFailed",
                            messages = result.Errors.Select(e => e.ErrorMessage).ToArray()
                        }, 400);
                        return;
                    }
                }
            }
        }

        await next();
    }
}