using FluentValidation;
using SimpleHttpListenerServer.Core;

namespace SimpleHttpListenerServer.Pipeline;

using FluentValidation.Results;

public sealed class ValidationMiddleware(IServiceProvider sp) : BaseMiddleware
{
    protected override async Task InvokeCoreAsync(HttpContextEx ctx, Func<Task> next, CancellationToken ct)
    {
        if (ctx.BoundModel is not null)
        {
            var modelType = ctx.BoundModel.GetType();
            var validatorType = typeof(AbstractValidator<>).MakeGenericType(modelType);
            var validator = sp.GetService(validatorType);

            if (validator is not null)
            {
                var validate = validator.GetType().GetMethod("Validate", [modelType]);
                var result = (ValidationResult)validate!.Invoke(validator, [ctx.BoundModel])!;
                if (!result.IsValid)
                {
                    throw new ValidationException(result.Errors.Select(e => e.ErrorMessage));
                }
            }
        }

        await next(); 
    }
}