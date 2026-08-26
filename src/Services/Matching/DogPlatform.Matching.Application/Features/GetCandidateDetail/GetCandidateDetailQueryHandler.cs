using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Evaluation;
using DogPlatform.Matching.Application.Features.SearchCandidates;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.GetCandidateDetail;

public sealed class GetCandidateDetailQueryHandler
    : IRequestHandler<GetCandidateDetailQuery, Result<CandidateSummaryResponse>>
{
    private readonly IMatchingProfileRepository _profileRepository;
    private readonly IFavoriteCandidateRepository _favoriteRepository;
    private readonly IPetsMatchingClient _petsClient;
    private readonly CandidateEvaluationService _evaluationService;
    private readonly ICurrentUser _currentUser;

    public GetCandidateDetailQueryHandler(
        IMatchingProfileRepository profileRepository,
        IFavoriteCandidateRepository favoriteRepository,
        IPetsMatchingClient petsClient,
        CandidateEvaluationService evaluationService,
        ICurrentUser currentUser)
    {
        _profileRepository = profileRepository;
        _favoriteRepository = favoriteRepository;
        _petsClient = petsClient;
        _evaluationService = evaluationService;
        _currentUser = currentUser;
    }

    public async Task<Result<CandidateSummaryResponse>> Handle(
        GetCandidateDetailQuery request, CancellationToken cancellationToken)
    {
        var sourcePet = await _petsClient.GetPetForMatchingAsync(request.PetId, cancellationToken);
        if (sourcePet is null || sourcePet.IsDeleted)
            return Result.Failure<CandidateSummaryResponse>(MatchingErrors.PetNotFound);

        if (sourcePet.OwnerId != _currentUser.UserId)
            return Result.Failure<CandidateSummaryResponse>(MatchingErrors.Unauthorized);

        var profile = await _profileRepository.GetActiveByPetIdAsync(request.PetId, cancellationToken);
        if (profile is null)
            return Result.Failure<CandidateSummaryResponse>(MatchingErrors.ProfileNotActive);

        var candidateProfile = await _profileRepository.GetActiveByPetIdAsync(
            request.CandidatePetId, cancellationToken);
        if (candidateProfile is null)
            return Result.Failure<CandidateSummaryResponse>(MatchingErrors.CandidateNotFound);

        var candidate = await _petsClient.GetPetForMatchingAsync(request.CandidatePetId, cancellationToken);
        if (candidate is null || candidate.IsDeleted)
            return Result.Failure<CandidateSummaryResponse>(MatchingErrors.CandidateNotFound);

        var evaluation = await _evaluationService.EvaluateAsync(
            sourcePet, candidate, profile, cancellationToken);

        if (evaluation.IsExcluded)
            return Result.Failure<CandidateSummaryResponse>(MatchingErrors.CandidateNotEligible);

        var isFavorite = await _favoriteRepository.ExistsAsync(
            request.PetId, request.CandidatePetId, cancellationToken);

        return Result.Success(new CandidateSummaryResponse(
            candidate.PetId,
            candidate.Name,
            candidate.BreedId,
            candidate.BreedName,
            candidate.Sex,
            candidate.AgeMonths,
            candidate.MainPhotoUrl,
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
            evaluation.Warnings,
            candidate.SpeciesName,
            candidate.Color,
            candidateProfile.Description,
            evaluation.GenealogyStatus is Domain.Enums.GenealogyValidationStatus.Unavailable
                or Domain.Enums.GenealogyValidationStatus.Unknown
                ? "Unknown"
                : evaluation.RelationshipType == Domain.Enums.RelationshipTypeSnapshot.UnrelatedWithinKnownPedigree
                    ? "NoKnownRelation"
                    : evaluation.RelationshipType == Domain.Enums.RelationshipTypeSnapshot.UnknownDueToIncompletePedigree
                        ? "Unknown" : "Related",
            evaluation.RelationshipType?.ToString(),
            candidate.PhotoUrls ?? (candidate.MainPhotoUrl is null ? [] : [candidate.MainPhotoUrl]),
            "La compatibilidad mostrada no reemplaza una evaluación veterinaria."));
    }
}
