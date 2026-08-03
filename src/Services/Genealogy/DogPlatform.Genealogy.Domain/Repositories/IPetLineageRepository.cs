using DogPlatform.Genealogy.Domain.Aggregates.PetLineage;

namespace DogPlatform.Genealogy.Domain.Repositories;

public interface IPetLineageRepository
{
    Task<PetLineage?> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken = default);

    Task AddAsync(PetLineage lineage, CancellationToken cancellationToken = default);

    Task UpdateAsync(PetLineage lineage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the lineage records for a batch of pet ids in a single query.
    /// Used to load one generation at a time during tree/ancestor/descendant traversals,
    /// avoiding N+1 SQL round-trips.
    /// </summary>
    Task<IReadOnlyList<PetLineage>> GetByPetIdsAsync(
        IEnumerable<Guid> petIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the lineage records of pets whose FatherId or MotherId is contained in
    /// <paramref name="parentIds"/>. Used for descendant traversal and sibling calculation
    /// in a single batched query per generation.
    /// </summary>
    Task<IReadOnlyList<PetLineage>> GetChildrenByParentIdsAsync(
        IEnumerable<Guid> parentIds,
        CancellationToken cancellationToken = default);
}
