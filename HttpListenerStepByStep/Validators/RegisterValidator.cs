using FluentValidation;
using HttpListenerStepByStep.Models;

namespace HttpListenerStepByStep.Validators;

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Login).Cascade(CascadeMode.Stop).NotEmpty()
            .Matches("^[a-z0-9_]{3,40}$").WithMessage("Логин: 3-40 символов a-z, 0-9, _");
        RuleFor(x => x.DisplayName).NotEmpty().WithMessage("Введите имя")
            .MaximumLength(80).WithMessage("Имя: не более 80 символов");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Введите пароль")
            .Length(12, 128).WithMessage("Пароль: 12-128 символов");
    }
}