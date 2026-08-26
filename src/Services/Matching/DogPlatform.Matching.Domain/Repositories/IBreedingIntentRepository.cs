using DogPlatform.Matching.Domain.Aggregates.BreedingIntent;

namespace DogPlatform.Matching.Domain.Repositories;

public interface IBreedingIntentRepository
{
    Task<BreedingIntent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BreedingIntent?> GetLatestByMatchIdAsync(Guid matchId,
        CancellationToken cancellationToken = default);
    Task<bool> HasOpenIntentAsync(Guid matchId, CancellationToken cancellationToken = default);
    void Add(BreedingIntent intent);
    void Update(BreedingIntent intent);
}
