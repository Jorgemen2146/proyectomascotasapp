using DogPlatform.Pets.Domain.Catalog;

namespace DogPlatform.Pets.Domain.Repositories;

public interface ISpeciesRepository
{
    Task<IReadOnlyCollection<Species>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int speciesId, CancellationToken cancellationToken = default);
}
