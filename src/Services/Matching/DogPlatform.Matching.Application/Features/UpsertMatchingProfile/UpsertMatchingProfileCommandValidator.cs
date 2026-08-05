using FluentValidation;

namespace DogPlatform.Matching.Application.Features.UpsertMatchingProfile;

public sealed class UpsertMatchingProfileCommandValidator : AbstractValidator<UpsertMatchingProfileCommand>
{
    public UpsertMatchingProfileCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.MinimumAgeMonths).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaximumAgeMonths).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumAgeMonths)
            .LessThanOrEqualTo(x => x.MaximumAgeMonths)
            .WithMessage("MinimumAgeMonths cannot be greater than MaximumAgeMonths.");
        RuleFor(x => x.MaximumEstimatedInbreedingCoefficient).InclusiveBetween(0, 1);
        RuleFor(x => x.MinimumCompatibilityScore).InclusiveBetween(0, 100);
        RuleForEach(x => x.PreferredBreedIds).GreaterThan(0);
    }
}
