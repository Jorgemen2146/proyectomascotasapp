using DogPlatform.Genealogy.Domain.Aggregates.PetLineage;

namespace DogPlatform.Genealogy.Domain.Repositories;

public interface IPetLineageRepository
{
    Task<PetLineage?> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken = default);

    Task AddAsync(PetLineage lineage, CancellationToken cancellationToken = default);

    Task UpdateAsync(PetLineage lineage, CancellationToken cancellationToken = default);
}
