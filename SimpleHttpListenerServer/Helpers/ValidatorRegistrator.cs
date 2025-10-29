
using FluentValidation;
using SimpleHttpListenerServer.Core;

namespace SimpleHttpListenerServer.Helpers;

public static class ValidatorRegistrator
{
    public static ServiceCollection RegisterValidators(this ServiceCollection services)
    {
        var assembly = typeof(UserValidationRules).Assembly;

        foreach (var type in assembly.GetTypes().Where(t =>
                     t is { IsClass: true, IsAbstract: false, BaseType.IsGenericType: true } &&
                     t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>)))
        {
            var baseType = type.BaseType!;
            services.AddTransient(baseType, type);
        }

        return services;
    }
}