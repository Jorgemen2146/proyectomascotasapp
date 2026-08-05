using DogPlatform.Matching.Domain.Enums;

namespace DogPlatform.Matching.Domain.Repositories;

public interface IMatchRequestRepository
{
    Task<Aggregates.MatchRequest.MatchRequest?> GetByIdAsync(
        Guid requestId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveRequestAsync(
        Guid requesterPetId, Guid candidatePetId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyCollection<Aggregates.MatchRequest.MatchRequest> Items, int TotalItems)> GetIncomingAsync(
        Guid candidateOwnerId,
        MatchRequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyCollection<Aggregates.MatchRequest.MatchRequest> Items, int TotalItems)> GetOutgoingAsync(
        Guid requesterOwnerId,
        MatchRequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(Aggregates.MatchRequest.MatchRequest request);

    void Update(Aggregates.MatchRequest.MatchRequest request);
}
