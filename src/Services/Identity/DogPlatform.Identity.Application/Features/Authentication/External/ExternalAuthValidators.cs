using FluentValidation;

namespace DogPlatform.Identity.Application.Features.Authentication.External;

public sealed class ExternalAuthCommandValidator : AbstractValidator<ExternalAuthCommand>
{
    public ExternalAuthCommandValidator()
    {
        RuleFor(x => x.Credential).NotEmpty().MaximumLength(16_384);
        When(x => x.Provider == Domain.Aggregates.ExternalLogin.ExternalAuthProvider.Apple, () =>
            RuleFor(x => x.Nonce).NotEmpty().MaximumLength(512));
    }
}

public sealed class CompleteExternalRegistrationCommandValidator
    : AbstractValidator<CompleteExternalRegistrationCommand>
{
    public CompleteExternalRegistrationCommandValidator()
    {
        RuleFor(x => x.RegistrationToken).NotEmpty().MaximumLength(16_384);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
    }
}

public sealed class LinkExternalLoginCommandValidator : AbstractValidator<LinkExternalLoginCommand>
{
    public LinkExternalLoginCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Credential).NotEmpty().MaximumLength(16_384);
        When(x => x.Provider == Domain.Aggregates.ExternalLogin.ExternalAuthProvider.Apple, () =>
            RuleFor(x => x.Nonce).NotEmpty().MaximumLength(512));
    }
}
