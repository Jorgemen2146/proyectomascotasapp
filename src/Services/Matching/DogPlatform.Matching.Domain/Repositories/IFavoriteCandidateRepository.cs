namespace DogPlatform.Matching.Domain.Repositories;

public interface IFavoriteCandidateRepository
{
    Task<Aggregates.FavoriteCandidate.FavoriteCandidate?> GetAsync(
        Guid sourcePetId, Guid candidatePetId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid sourcePetId, Guid candidatePetId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyCollection<Aggregates.FavoriteCandidate.FavoriteCandidate> Items, int TotalItems)> GetPagedAsync(
        Guid sourcePetId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    void Add(Aggregates.FavoriteCandidate.FavoriteCandidate favorite);

    void Remove(Aggregates.FavoriteCandidate.FavoriteCandidate favorite);
}
