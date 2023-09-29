using FluentValidation;
using FluentValidation.Validators;
using HttpListenerExample.Entities;

namespace HttpListenerExample.Helpers;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(u => u.Email)
            .NotEmpty()
            .EmailAddress()
            .NotNull().WithMessage("Почта введена не корректно!");
        RuleFor(u => u.Password)
            .MatchPassword()
            .WithMessage("Пароль не соответствует требованиям!");
    }
}

public static class CustomValidators
{
    public static IRuleBuilderOptions<T, string> MatchPassword<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.SetValidator(new RegularExpressionValidator<T>(@"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9]).{8,}$"));
    }
}