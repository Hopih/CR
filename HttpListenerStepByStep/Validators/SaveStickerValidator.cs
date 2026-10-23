using FluentValidation;
using HttpListenerStepByStep.Models;

namespace HttpListenerStepByStep.Validators;

public sealed class SaveStickerValidator : AbstractValidator<SaveStickerRequest>
{
    public SaveStickerValidator()
    {
        RuleFor(x => x.Text).NotNull().MaximumLength(2000);
        RuleFor(x => x.X).InclusiveBetween(0, 10000);
        RuleFor(x => x.Y).InclusiveBetween(0, 10000);
        RuleFor(x => x.Width).InclusiveBetween(80, 10000);
        RuleFor(x => x.Height).InclusiveBetween(80, 10000);
    }
}