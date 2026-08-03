using DogPlatform.Genealogy.Application.Options;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace DogPlatform.Genealogy.Application.Features.CalculateRelationship;

public sealed class CalculateRelationshipQueryValidator : AbstractValidator<CalculateRelationshipQuery>
{
    public CalculateRelationshipQueryValidator(IOptions<GenealogyAnalysisOptions> options)
    {
        var maxDepth = options.Value.MaximumAnalysisDepth;

        RuleFor(x => x.PetId1).NotEmpty().WithMessage("PetId1 is required.");
        RuleFor(x => x.PetId2).NotEmpty().WithMessage("PetId2 is required.");

        RuleFor(x => x.Depth)
            .InclusiveBetween(1, maxDepth)
            .When(x => x.Depth.HasValue)
            .WithMessage($"Depth must be between 1 and {maxDepth}.");
    }
}
