using DogPlatform.Genealogy.Application.Analysis;
using DogPlatform.Genealogy.Application.Options;
using DogPlatform.Genealogy.Application.Security;
using DogPlatform.Genealogy.Application.Services;
using DogPlatform.Genealogy.Application.Traversal;
using DogPlatform.Genealogy.Domain.Errors;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.Extensions.Options;

namespace DogPlatform.Genealogy.Application.Features.GetLineageStatistics;

public sealed class GetLineageStatisticsQueryHandler
    : IRequestHandler<GetLineageStatisticsQuery, Result<LineageStatisticsResponse>>
{
    private readonly IGenealogyTraversalService _traversal;
    private readonly IPedigreeStatisticsCalculator _statisticsCalculator;
    private readonly IPetVerificationService _petVerification;
    private readonly ICurrentUser _currentUser;
    private readonly GenealogyAnalysisOptions _options;

    public GetLineageStatisticsQueryHandler(
        IGenealogyTraversalService traversal,
        IPedigreeStatisticsCalculator statisticsCalculator,
        IPetVerificationService petVerification,
        ICurrentUser currentUser,
        IOptions<GenealogyAnalysisOptions> options)
    {
        _traversal            = traversal;
        _statisticsCalculator = statisticsCalculator;
        _petVerification      = petVerification;
        _currentUser          = currentUser;
        _options              = options.Value;
    }

    public async Task<Result<LineageStatisticsResponse>> Handle(
        GetLineageStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var depth = Math.Clamp(request.Depth ?? _options.DefaultAnalysisDepth, 1, _options.MaximumAnalysisDepth);

        // Privacy policy: only the owner of the pet may query its pedigree statistics.
        var owns = await _petVerification.PetBelongsToOwnerAsync(
            request.PetId, _currentUser.UserId, cancellationToken);

        if (!owns)
            return Result.Failure<LineageStatisticsResponse>(GenealogyErrors.Unauthorized);

        var graph = await _traversal.BuildAncestorGraphAsync(request.PetId, depth, maxNodes: 1000, cancellationToken);

        var stats = _statisticsCalculator.Calculate(request.PetId, graph, depth);

        var response = new LineageStatisticsResponse(
            PetId: stats.PetId,
            RequestedDepth: stats.RequestedDepth,
            ProcessedDepth: stats.ProcessedDepth,
            TotalPositions: stats.TotalPositions,
            KnownAncestorPositions: stats.KnownAncestorPositions,
            MissingAncestorPositions: stats.MissingAncestorPositions,
            UniqueAncestorCount: stats.UniqueAncestorCount,
            RepeatedAncestorCount: stats.RepeatedAncestorCount,
            PedigreeCompletenessPercentage: stats.PedigreeCompletenessPercentage,
            AncestorsByGeneration: stats.AncestorsByGeneration
                .Select(g => new GenerationDistributionResponse(g.Generation, g.ExpectedPositions, g.KnownPositions))
                .ToList(),
            RepeatedAncestors: stats.RepeatedAncestors
                .Select(r => new RepeatedAncestorResponse(
                    r.AncestorPetId, r.OccurrenceCount, r.Generations, r.LineagePaths, r.Contribution))
                .ToList(),
            EstimatedInbreedingCoefficientPercentage: stats.EstimatedInbreedingCoefficientPercentage,
            CalculationMethod: stats.CalculationMethod,
            Warnings: stats.Warnings);

        return Result.Success(response);
    }
}
