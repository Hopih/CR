using FluentValidation;
using HttpListenerStepByStep.Models;

namespace HttpListenerStepByStep.Validators;

public class CreateBoardValidator : AbstractValidator<CreateBoardRequest>
{
    public CreateBoardValidator()
    {
        RuleFor(x => x.Title)
            .Length(1, 120).WithMessage(
                "Title must be more than 1 chars and less than 120 chars")
            .NotEmpty().WithMessage("Title must be more than 0 chars");
    }
}