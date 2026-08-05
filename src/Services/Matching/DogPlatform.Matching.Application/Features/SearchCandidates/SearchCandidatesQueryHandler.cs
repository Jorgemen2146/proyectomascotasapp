using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Common;
using DogPlatform.Matching.Application.Evaluation;
using DogPlatform.Matching.Application.Options;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.Extensions.Options;

namespace DogPlatform.Matching.Application.Features.SearchCandidates;

public sealed class SearchCandidatesQueryHandler
    : IRequestHandler<SearchCandidatesQuery, Result<PagedResult<CandidateSummaryResponse>>>
{
    private readonly IMatchingProfileRepository _profileRepository;
    private readonly IFavoriteCandidateRepository _favoriteRepository;
    private readonly IPetsMatchingClient _petsClient;
    private readonly CandidateEvaluationService _evaluationService;
    private readonly ICurrentUser _currentUser;
    private readonly MatchingOptions _options;

    public SearchCandidatesQueryHandler(
        IMatchingProfileRepository profileRepository,
        IFavoriteCandidateRepository favoriteRepository,
        IPetsMatchingClient petsClient,
        CandidateEvaluationService evaluationService,
        ICurrentUser currentUser,
        IOptions<MatchingOptions> options)
    {
        _profileRepository = profileRepository;
        _favoriteRepository = favoriteRepository;
        _petsClient = petsClient;
        _evaluationService = evaluationService;
        _currentUser = currentUser;
        _options = options.Value;
    }

    public async Task<Result<PagedResult<CandidateSummaryResponse>>> Handle(
        SearchCandidatesQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, _options.MaximumPageSize);

        var sourcePet = await _petsClient.GetPetForMatchingAsync(request.PetId, cancellationToken);
        if (sourcePet is null || sourcePet.IsDeleted)
            return Result.Failure<PagedResult<CandidateSummaryResponse>>(MatchingErrors.PetNotFound);

        if (sourcePet.OwnerId != _currentUser.UserId)
            return Result.Failure<PagedResult<CandidateSummaryResponse>>(MatchingErrors.Unauthorized);

        var profile = await _profileRepository.GetActiveByPetIdAsync(request.PetId, cancellationToken);
        if (profile is null)
            return Result.Failure<PagedResult<CandidateSummaryResponse>>(MatchingErrors.ProfileNotActive);

        var requiredSex = string.Equals(sourcePet.Sex, "M", StringComparison.OrdinalIgnoreCase) ? "F" : "M";

        var filter = new CandidateSearchFilter(
            sourcePet.OwnerId,
            requiredSex,
            request.BreedId,
            request.MinimumAgeMonths ?? profile.MinimumAgeMonths,
            request.MaximumAgeMonths ?? profile.MaximumAgeMonths,
            1,
            _options.MaximumCandidatesEvaluatedPerSearch);

        var preliminaryPage = await _petsClient.SearchCandidatesAsync(filter, cancellationToken);
        if (preliminaryPage is null)
            return Result.Failure<PagedResult<CandidateSummaryResponse>>(MatchingErrors.PetsServiceUnavailable);

        // Bounded concurrency when evaluating candidates against Genealogy/Health,
        // to avoid unbounded fan-out for large preliminary pages.
        using var semaphore = new SemaphoreSlim(_options.GenealogyEvaluationConcurrency);
        var evaluationTasks = preliminaryPage.Items.Select(async candidate =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await _evaluationService.EvaluateAsync(sourcePet, candidate, profile, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var evaluations = await Task.WhenAll(evaluationTasks);

        var eligible = evaluations.Where(e => !e.IsExcluded).ToList();

        if (request.MinimumScore.HasValue)
            eligible = eligible.Where(e => e.Score!.TotalScore >= request.MinimumScore.Value).ToList();

        if (request.FavoritesOnly)
        {
            var (favorites, _) = await _favoriteRepository.GetPagedAsync(
                request.PetId, 1, int.MaxValue, cancellationToken);
            var favoriteIds = favorites.Select(f => f.CandidatePetId).ToHashSet();
            eligible = eligible.Where(e => favoriteIds.Contains(e.Candidate.PetId)).ToList();
        }

        // Deterministic ordering: by score DESC then PetId ASC as tiebreaker.
        var ordered = request.SortDirection.Equals("ASC", StringComparison.OrdinalIgnoreCase)
            ? eligible.OrderBy(e => e.Score!.TotalScore).ThenBy(e => e.Candidate.PetId)
            : eligible.OrderByDescending(e => e.Score!.TotalScore).ThenBy(e => e.Candidate.PetId);

        var totalItems = ordered.Count();
        var skip = (request.PageNumber - 1) * pageSize;
        var pageItems = ordered.Skip(skip).Take(pageSize).ToList();

        var favoriteLookup = new HashSet<Guid>();
        if (pageItems.Count > 0)
        {
            var (favorites, _) = await _favoriteRepository.GetPagedAsync(
                request.PetId, 1, int.MaxValue, cancellationToken);
            favoriteLookup = favorites.Select(f => f.CandidatePetId).ToHashSet();
        }

        var responseItems = pageItems.Select(e => Map(e, favoriteLookup.Contains(e.Candidate.PetId))).ToList();

        return Result.Success(
            PagedResult<CandidateSummaryResponse>.Create(responseItems, totalItems, request.PageNumber, pageSize));
    }

    private static CandidateSummaryResponse Map(CandidateEvaluation evaluation, bool isFavorite) =>
        new(
            evaluation.Candidate.PetId,
            evaluation.Candidate.Name,
            evaluation.Candidate.BreedId,
            evaluation.Candidate.BreedName,
            evaluation.Candidate.Sex,
            evaluation.Candidate.AgeMonths,
            evaluation.Candidate.MainPhotoUrl,
            evaluation.Score!.TotalScore,
            new CompatibilityBreakdownResponse(
                evaluation.Score.BreedScore,
                evaluation.Score.AgeScore,
                evaluation.Score.PedigreeScore,
                evaluation.Score.GenealogyScore,
                evaluation.Score.HealthScore),
            evaluation.PedigreeCompletenessPercentage,
            evaluation.RelationshipType,
            evaluation.EstimatedOffspringInbreedingCoefficient,
            evaluation.GenealogyStatus,
            evaluation.Health.Status,
            isFavorite,
            evaluation.Warnings);
}
