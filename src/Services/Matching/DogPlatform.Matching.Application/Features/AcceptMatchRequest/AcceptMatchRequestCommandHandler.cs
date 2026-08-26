using DogPlatform.Matching.Application.Features.Common;
using DogPlatform.Matching.Application.Clients.Notifications;
using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.AcceptMatchRequest;

public sealed class AcceptMatchRequestCommandHandler
    : IRequestHandler<AcceptMatchRequestCommand, Result<MatchRequestResponse>>
{
    private readonly IMatchRequestRepository _requestRepository;
    private readonly IMatchingUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IPetMatchRepository _matchRepository;
    private readonly IMatchingNotificationClient _notificationClient;
    private readonly IPetsMatchingClient _petsClient;

    public AcceptMatchRequestCommandHandler(
        IMatchRequestRepository requestRepository,
        IMatchingUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IPetMatchRepository matchRepository,
        IMatchingNotificationClient notificationClient,
        IPetsMatchingClient petsClient)
    {
        _requestRepository = requestRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _matchRepository = matchRepository;
        _notificationClient = notificationClient;
        _petsClient = petsClient;
    }

    public async Task<Result<MatchRequestResponse>> Handle(
        AcceptMatchRequestCommand request, CancellationToken cancellationToken)
    {
        var matchRequest = await _requestRepository.GetByIdAsync(request.MatchRequestId, cancellationToken);
        if (matchRequest is null)
            return Result.Failure<MatchRequestResponse>(MatchingErrors.RequestNotFound);

        var result = matchRequest.Accept(_currentUser.UserId, _timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
            return Result.Failure<MatchRequestResponse>(result.Error);

        var match = Domain.Aggregates.PetMatch.PetMatch.Create(
            matchRequest.Id,
            matchRequest.RequesterPetId,
            matchRequest.CandidatePetId,
            matchRequest.RequesterOwnerId,
            matchRequest.CandidateOwnerId,
            matchRequest.RequesterSharePhoneNumber,
            request.SharePhoneNumber,
            _timeProvider.GetUtcNow().UtcDateTime);

        _requestRepository.Update(matchRequest);
        _matchRepository.Add(match);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var candidate = await _petsClient.GetPetForMatchingAsync(
            matchRequest.CandidatePetId, cancellationToken);
        await _notificationClient.SendAsync(new MatchingNotification(
            matchRequest.RequesterOwnerId,
            "MatchingRequestAccepted",
            "Solicitud de matching aceptada",
            "La otra mascota aceptó tu solicitud de matching.",
            matchRequest.Id,
            matchRequest.CandidatePetId,
            candidate?.MainPhotoUrl), cancellationToken);

        return Result.Success(Map(matchRequest));
    }

    private static MatchRequestResponse Map(Domain.Aggregates.MatchRequest.MatchRequest r) =>
        new(
            r.Id, r.RequesterPetId, r.CandidatePetId, r.Status, r.Message,
            r.CompatibilityScoreSnapshot, r.EstimatedInbreedingCoefficientSnapshot,
            r.RelationshipTypeSnapshot, r.CreatedAt, r.UpdatedAt, r.RespondedAt, r.CancelledAt, r.ExpiresAt);
}
