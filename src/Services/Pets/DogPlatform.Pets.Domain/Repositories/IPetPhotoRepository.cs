using DogPlatform.Pets.Domain.Aggregates.Pet;

namespace DogPlatform.Pets.Domain.Repositories;

public interface IPetPhotoRepository
{
    Task<IReadOnlyCollection<PetPhoto>> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken = default);

    Task<PetPhoto?> GetByIdAsync(Guid photoId, CancellationToken cancellationToken = default);

    Task AddAsync(PetPhoto photo, CancellationToken cancellationToken = default);

    Task RemoveAsync(PetPhoto photo, CancellationToken cancellationToken = default);
}
