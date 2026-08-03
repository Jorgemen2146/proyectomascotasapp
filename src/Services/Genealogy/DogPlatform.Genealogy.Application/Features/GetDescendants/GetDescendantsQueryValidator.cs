using DogPlatform.Genealogy.Application.Options;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace DogPlatform.Genealogy.Application.Features.GetDescendants;

public sealed class GetDescendantsQueryValidator : AbstractValidator<GetDescendantsQuery>
{
    public GetDescendantsQueryValidator(IOptions<GenealogyOptions> options)
    {
        var maxDepth = options.Value.MaximumTreeDepth;

        RuleFor(x => x.PetId)
            .NotEmpty().WithMessage("PetId is required.");

        RuleFor(x => x.Depth)
            .InclusiveBetween(1, maxDepth)
            .When(x => x.Depth.HasValue)
            .WithMessage($"Depth must be between 1 and {maxDepth}.");
    }
}
