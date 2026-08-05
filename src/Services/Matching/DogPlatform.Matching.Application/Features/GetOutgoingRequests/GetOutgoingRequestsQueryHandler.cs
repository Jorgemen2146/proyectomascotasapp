using DogPlatform.Matching.Application.Common;
using DogPlatform.Matching.Application.Features.Common;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.GetOutgoingRequests;

public sealed class GetOutgoingRequestsQueryHandler
    : IRequestHandler<GetOutgoingRequestsQuery, Result<PagedResult<MatchRequestResponse>>>
{
    private readonly IMatchRequestRepository _requestRepository;
    private readonly ICurrentUser _currentUser;

    public GetOutgoingRequestsQueryHandler(
        IMatchRequestRepository requestRepository, ICurrentUser currentUser)
    {
        _requestRepository = requestRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<MatchRequestResponse>>> Handle(
        GetOutgoingRequestsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalItems) = await _requestRepository.GetOutgoingAsync(
            _currentUser.UserId, request.Status, request.PageNumber, request.PageSize, cancellationToken);

        var responses = items.Select(Map).ToList();

        return Result.Success(
            PagedResult<MatchRequestResponse>.Create(responses, totalItems, request.PageNumber, request.PageSize));
    }

    private static MatchRequestResponse Map(Domain.Aggregates.MatchRequest.MatchRequest r) =>
        new(
            r.Id, r.RequesterPetId, r.CandidatePetId, r.Status, r.Message,
            r.CompatibilityScoreSnapshot, r.EstimatedInbreedingCoefficientSnapshot,
            r.RelationshipTypeSnapshot, r.CreatedAt, r.UpdatedAt, r.RespondedAt, r.CancelledAt, r.ExpiresAt);
}
