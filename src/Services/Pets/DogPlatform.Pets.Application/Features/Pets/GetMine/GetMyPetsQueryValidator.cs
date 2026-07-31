using DogPlatform.Pets.Domain.ValueObjects;
using FluentValidation;

namespace DogPlatform.Pets.Application.Features.Pets.GetMine;

public sealed class GetMyPetsQueryValidator : AbstractValidator<GetMyPetsQuery>
{
    private static readonly HashSet<string> AllowedSortBy =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Name", "BirthDate", "CreatedAt", "UpdatedAt"
        };

    private static readonly HashSet<string> AllowedSortDirection =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ASC", "DESC"
        };

    public GetMyPetsQueryValidator()
    {
        RuleFor(q => q.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be at least 1.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(q => q.SortBy)
            .Must(s => AllowedSortBy.Contains(s))
            .WithMessage("SortBy must be one of: Name, BirthDate, CreatedAt, UpdatedAt.");

        RuleFor(q => q.SortDirection)
            .Must(s => AllowedSortDirection.Contains(s))
            .WithMessage("SortDirection must be ASC or DESC.");

        When(q => q.SpeciesId.HasValue, () =>
            RuleFor(q => q.SpeciesId!.Value)
                .GreaterThan(0)
                .WithMessage("SpeciesId must be greater than 0."));

        When(q => q.BreedId.HasValue, () =>
            RuleFor(q => q.BreedId!.Value)
                .GreaterThan(0)
                .WithMessage("BreedId must be greater than 0."));

        When(q => !string.IsNullOrWhiteSpace(q.Sex), () =>
            RuleFor(q => q.Sex)
                .Must(s => Gender.Create(s).IsSuccess)
                .WithMessage($"Sex must be '{Gender.Male}' or '{Gender.Female}'."));
    }
}
