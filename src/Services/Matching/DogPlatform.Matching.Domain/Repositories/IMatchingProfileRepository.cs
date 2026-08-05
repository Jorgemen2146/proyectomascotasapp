namespace DogPlatform.Matching.Domain.Repositories;

public interface IMatchingProfileRepository
{
    Task<Aggregates.MatchingProfile.MatchingProfile?> GetByPetIdAsync(
        Guid petId, CancellationToken cancellationToken = default);

    Task<Aggregates.MatchingProfile.MatchingProfile?> GetActiveByPetIdAsync(
        Guid petId, CancellationToken cancellationToken = default);

    void Add(Aggregates.MatchingProfile.MatchingProfile profile);

    void Update(Aggregates.MatchingProfile.MatchingProfile profile);
}
