using DogPlatform.Matching.Application.Common;
using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Features.Matches;
using DogPlatform.Matching.Application.Features.Common;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.GetIncomingRequests;

public sealed class GetIncomingRequestsQueryHandler
    : IRequestHandler<GetIncomingRequestsQuery, Result<PagedResult<MatchRequestResponse>>>
{
    private readonly IMatchRequestRepository _requestRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPetsMatchingClient _petsClient;

    public GetIncomingRequestsQueryHandler(
        IMatchRequestRepository requestRepository, ICurrentUser currentUser,
        IPetsMatchingClient petsClient)
    {
        _requestRepository = requestRepository;
        _currentUser = currentUser;
        _petsClient = petsClient;
    }

    public async Task<Result<PagedResult<MatchRequestResponse>>> Handle(
        GetIncomingRequestsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalItems) = await _requestRepository.GetIncomingAsync(
            _currentUser.UserId, request.Status, request.PageNumber, request.PageSize, cancellationToken);

        var pets = await _petsClient.GetPetsByIdsAsync(items
            .SelectMany(item => new[] { item.RequesterPetId, item.CandidatePetId })
            .Distinct().ToArray(), cancellationToken);
        var byId = pets.ToDictionary(pet => pet.PetId);
        var responses = items.Select(item => Map(item, byId)).ToList();

        return Result.Success(
            PagedResult<MatchRequestResponse>.Create(responses, totalItems, request.PageNumber, request.PageSize));
    }

    private static MatchRequestResponse Map(Domain.Aggregates.MatchRequest.MatchRequest r,
        IReadOnlyDictionary<Guid, PetMatchingDataResponse> pets) =>
        new(
            r.Id, r.RequesterPetId, r.CandidatePetId, r.Status, r.Message,
            r.CompatibilityScoreSnapshot, r.EstimatedInbreedingCoefficientSnapshot,
            r.RelationshipTypeSnapshot, r.CreatedAt, r.UpdatedAt, r.RespondedAt, r.CancelledAt, r.ExpiresAt,
            pets.TryGetValue(r.RequesterPetId, out var requester) ? MatchMapping.PublicPet(requester) : null,
            pets.TryGetValue(r.CandidatePetId, out var target) ? MatchMapping.PublicPet(target) : null);
}
