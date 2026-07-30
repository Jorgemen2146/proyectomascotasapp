using DogPlatform.Pets.Domain.Aggregates.Pet;
using DogPlatform.Pets.Domain.Catalog;

namespace DogPlatform.Pets.Domain.Repositories;

public interface IPetRepository
{
    Task<Pet?> GetByIdAsync(Guid petId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Pet>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Pet pet, CancellationToken cancellationToken = default);

    Task UpdateAsync(Pet pet, CancellationToken cancellationToken = default);
}

public interface IBreedRepository
{
    Task<Breed?> GetByIdAsync(int breedId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Breed>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Breed>> GetBySpeciesIdAsync(
        int speciesId,
        CancellationToken cancellationToken = default);
}
