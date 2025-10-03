using FluentValidation;
using SimpleHttpListenerServer.Entities;
using SimpleHttpListenerServer.Models;

namespace SimpleHttpListenerServer;

public class UserValidationRules:AbstractValidator<SignupRequest>
{
    public UserValidationRules()
    {
        RuleFor(user => user.Login)
            .NotEmpty()
            .Matches("^[a-zA-Z0-9_]{5,20}$")
            .WithMessage("Username must be between 5 and 20 characters.");
        RuleFor(user => user.Password)
            .NotEmpty()
            .Matches(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")
            .WithMessage("Password must be between 8 and 20 characters, at least one digit, special symbol, and upper case letter.");
    }
}