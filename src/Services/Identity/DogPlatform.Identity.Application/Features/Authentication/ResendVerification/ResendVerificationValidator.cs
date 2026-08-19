using FluentValidation;

namespace DogPlatform.Identity.Application.Features.Authentication.ResendVerification;

public sealed class ResendVerificationValidator : AbstractValidator<ResendVerificationCommand>
{
    public ResendVerificationValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(200)
            .EmailAddress();
    }
}
