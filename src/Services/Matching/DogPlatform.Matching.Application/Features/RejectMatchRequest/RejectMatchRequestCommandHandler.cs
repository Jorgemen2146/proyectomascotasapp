using DogPlatform.Matching.Application.Features.Common;
using DogPlatform.Matching.Application.Clients.Notifications;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.RejectMatchRequest;

public sealed class RejectMatchRequestCommandHandler
    : IRequestHandler<RejectMatchRequestCommand, Result<MatchRequestResponse>>
{
    private readonly IMatchRequestRepository _requestRepository;
    private readonly IMatchingUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IMatchingNotificationClient _notificationClient;

    public RejectMatchRequestCommandHandler(
        IMatchRequestRepository requestRepository,
        IMatchingUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IMatchingNotificationClient notificationClient)
    {
        _requestRepository = requestRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _notificationClient = notificationClient;
    }

    public async Task<Result<MatchRequestResponse>> Handle(
        RejectMatchRequestCommand request, CancellationToken cancellationToken)
    {
        var matchRequest = await _requestRepository.GetByIdAsync(request.MatchRequestId, cancellationToken);
        if (matchRequest is null)
            return Result.Failure<MatchRequestResponse>(MatchingErrors.RequestNotFound);

        var result = matchRequest.Reject(_currentUser.UserId, _timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
            return Result.Failure<MatchRequestResponse>(result.Error);

        _requestRepository.Update(matchRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationClient.SendAsync(new MatchingNotification(
            matchRequest.RequesterOwnerId,
            "MatchingRequestRejected",
            "Solicitud de matching rechazada",
            "La otra mascota rechazó tu solicitud de matching.",
            matchRequest.Id,
            matchRequest.CandidatePetId,
            null), cancellationToken);

        return Result.Success(Map(matchRequest));
    }

    private static MatchRequestResponse Map(Domain.Aggregates.MatchRequest.MatchRequest r) =>
        new(
            r.Id, r.RequesterPetId, r.CandidatePetId, r.Status, r.Message,
            r.CompatibilityScoreSnapshot, r.EstimatedInbreedingCoefficientSnapshot,
            r.RelationshipTypeSnapshot, r.CreatedAt, r.UpdatedAt, r.RespondedAt, r.CancelledAt, r.ExpiresAt);
}
