using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Clients.Notifications;
using DogPlatform.Matching.Application.Evaluation;
using DogPlatform.Matching.Application.Features.Common;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.CreateMatchRequest;

public sealed class CreateMatchRequestCommandHandler
    : IRequestHandler<CreateMatchRequestCommand, Result<MatchRequestResponse>>
{
    private readonly IMatchingProfileRepository _profileRepository;
    private readonly IMatchRequestRepository _requestRepository;
    private readonly IMatchingUnitOfWork _unitOfWork;
    private readonly IPetsMatchingClient _petsClient;
    private readonly CandidateEvaluationService _evaluationService;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IMatchingNotificationClient _notificationClient;

    public CreateMatchRequestCommandHandler(
        IMatchingProfileRepository profileRepository,
        IMatchRequestRepository requestRepository,
        IMatchingUnitOfWork unitOfWork,
        IPetsMatchingClient petsClient,
        CandidateEvaluationService evaluationService,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IMatchingNotificationClient notificationClient)
    {
        _profileRepository = profileRepository;
        _requestRepository = requestRepository;
        _unitOfWork = unitOfWork;
        _petsClient = petsClient;
        _evaluationService = evaluationService;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _notificationClient = notificationClient;
    }

    public async Task<Result<MatchRequestResponse>> Handle(
        CreateMatchRequestCommand request, CancellationToken cancellationToken)
    {
        var sourcePet = await _petsClient.GetPetForMatchingAsync(request.PetId, cancellationToken);
        if (sourcePet is null || sourcePet.IsDeleted)
            return Result.Failure<MatchRequestResponse>(MatchingErrors.PetNotFound);

        if (sourcePet.OwnerId != _currentUser.UserId)
            return Result.Failure<MatchRequestResponse>(MatchingErrors.Unauthorized);

        if (sourcePet.IsSterilized)
            return Result.Failure<MatchRequestResponse>(MatchingErrors.MatchingNotCompatible);

        var profile = await _profileRepository.GetActiveByPetIdAsync(request.PetId, cancellationToken);
        if (profile is null)
            return Result.Failure<MatchRequestResponse>(MatchingErrors.ProfileNotActive);

        var candidateProfile = await _profileRepository.GetActiveByPetIdAsync(
            request.CandidatePetId, cancellationToken);
        if (candidateProfile is null)
            return Result.Failure<MatchRequestResponse>(MatchingErrors.CandidateNotFound);

        var candidate = await _petsClient.GetPetForMatchingAsync(request.CandidatePetId, cancellationToken);
        if (candidate is null || candidate.IsDeleted)
            return Result.Failure<MatchRequestResponse>(MatchingErrors.CandidateNotFound);

        if (candidate.IsSterilized)
            return Result.Failure<MatchRequestResponse>(MatchingErrors.MatchingNotCompatible);

        if (candidate.OwnerId == sourcePet.OwnerId || candidate.PetId == sourcePet.PetId)
            return Result.Failure<MatchRequestResponse>(MatchingErrors.MatchingSelfRequest);

        var hasActiveRequest = await _requestRepository.HasActiveRequestAsync(
            request.PetId, request.CandidatePetId, cancellationToken);
        if (hasActiveRequest)
            return Result.Failure<MatchRequestResponse>(MatchingErrors.MatchingRequestExists);

        // Re-verify compatibility at the moment of the request; reject if the
        // candidate no longer qualifies (exclusion rules re-applied).
        var evaluation = await _evaluationService.EvaluateAsync(
            sourcePet, candidate, profile, cancellationToken);

        if (evaluation.IsExcluded)
            return Result.Failure<MatchRequestResponse>(MatchingErrors.MatchingNotCompatible);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        var creation = Domain.Aggregates.MatchRequest.MatchRequest.Create(
            request.PetId,
            sourcePet.OwnerId,
            request.CandidatePetId,
            candidate.OwnerId,
            request.Message,
            evaluation.Score!.TotalScore,
            evaluation.EstimatedOffspringInbreedingCoefficient ?? 0,
            evaluation.RelationshipType ?? Domain.Enums.RelationshipTypeSnapshot.UnrelatedWithinKnownPedigree,
            request.SharePhoneNumber,
            utcNow);

        if (creation.IsFailure)
            return Result.Failure<MatchRequestResponse>(creation.Error);

        _requestRepository.Add(creation.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationClient.SendAsync(new MatchingNotification(
            candidate.OwnerId,
            "MatchingRequestReceived",
            "Nueva solicitud de matching",
            $"Una mascota está interesada en conocer a {candidate.Name}.",
            creation.Value.Id,
            sourcePet.PetId,
            sourcePet.MainPhotoUrl), cancellationToken);

        return Result.Success(Map(creation.Value));
    }

    private static MatchRequestResponse Map(Domain.Aggregates.MatchRequest.MatchRequest r) =>
        new(
            r.Id,
            r.RequesterPetId,
            r.CandidatePetId,
            r.Status,
            r.Message,
            r.CompatibilityScoreSnapshot,
            r.EstimatedInbreedingCoefficientSnapshot,
            r.RelationshipTypeSnapshot,
            r.CreatedAt,
            r.UpdatedAt,
            r.RespondedAt,
            r.CancelledAt,
            r.ExpiresAt);
}
