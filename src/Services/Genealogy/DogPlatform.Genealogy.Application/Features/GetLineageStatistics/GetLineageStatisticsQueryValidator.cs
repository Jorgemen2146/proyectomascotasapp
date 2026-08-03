using DogPlatform.Genealogy.Application.Options;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace DogPlatform.Genealogy.Application.Features.GetLineageStatistics;

public sealed class GetLineageStatisticsQueryValidator : AbstractValidator<GetLineageStatisticsQuery>
{
    public GetLineageStatisticsQueryValidator(IOptions<GenealogyAnalysisOptions> options)
    {
        var maxDepth = options.Value.MaximumAnalysisDepth;

        RuleFor(x => x.PetId)
            .NotEmpty().WithMessage("PetId is required.");

        RuleFor(x => x.Depth)
            .InclusiveBetween(1, maxDepth)
            .When(x => x.Depth.HasValue)
            .WithMessage($"Depth must be between 1 and {maxDepth}.");
    }
}
