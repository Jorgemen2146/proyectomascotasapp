using DogPlatform.Matching.Domain.Aggregates.PetMatch;

namespace DogPlatform.Matching.Domain.Repositories;

public interface IPetMatchRepository
{
    Task<PetMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PetMatch?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PetMatch>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    void Add(PetMatch match);
}
