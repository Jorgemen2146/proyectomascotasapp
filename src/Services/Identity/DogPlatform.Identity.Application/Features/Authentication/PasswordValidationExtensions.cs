using FluentValidation;

namespace DogPlatform.Identity.Application.Features.Authentication;

public static class PasswordValidationExtensions
{
    public static IRuleBuilderOptions<T, string> ApplyDogPlatformPasswordPolicy<T>(
        this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");
}
