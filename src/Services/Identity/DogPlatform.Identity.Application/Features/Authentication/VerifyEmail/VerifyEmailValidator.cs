using FluentValidation;

namespace DogPlatform.Identity.Application.Features.Authentication.VerifyEmail;

public sealed class VerifyEmailValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(200)
            .EmailAddress();

        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches("^[0-9]{6}$")
            .WithMessage("Code must contain exactly 6 digits.");
    }
}
