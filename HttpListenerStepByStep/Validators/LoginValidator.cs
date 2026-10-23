using FluentValidation;
using HttpListenerStepByStep.Models;

namespace HttpListenerStepByStep.Validators;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Login).Cascade(CascadeMode.Stop).NotEmpty()
            .Matches("^[a-z0-9_]{3,40}$");
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}