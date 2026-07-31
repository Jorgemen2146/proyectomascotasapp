using FluentValidation;

namespace DogPlatform.Genealogy.Application.Features.AssignParents;

public sealed class AssignParentsCommandValidator : AbstractValidator<AssignParentsCommand>
{
    public AssignParentsCommandValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty().WithMessage("PetId is required.");

        RuleFor(x => x.FatherId)
            .NotEqual(x => x.PetId)
            .When(x => x.FatherId.HasValue)
            .WithMessage("Father cannot be the same pet.");

        RuleFor(x => x.MotherId)
            .NotEqual(x => x.PetId)
            .When(x => x.MotherId.HasValue)
            .WithMessage("Mother cannot be the same pet.");

        RuleFor(x => x)
            .Must(x => x.FatherId != x.MotherId || (!x.FatherId.HasValue && !x.MotherId.HasValue))
            .WithMessage("Father and mother cannot be the same pet.");
    }
}
