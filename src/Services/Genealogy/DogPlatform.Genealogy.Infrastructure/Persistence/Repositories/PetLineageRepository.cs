using DogPlatform.Genealogy.Domain.Aggregates.PetLineage;
using DogPlatform.Genealogy.Domain.Relationships;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.Genealogy.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Genealogy.Infrastructure.Persistence.Repositories;

public sealed class PetLineageRepository : IPetLineageRepository
{
    private readonly GenealogyDbContext _context;

    public PetLineageRepository(GenealogyDbContext context)
    {
        _context = context;
    }

    public async Task<PetLineage?> GetByPetIdAsync(
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        var relationships = await ActiveRelationships()
            .Where(item => item.ChildPetId == petId).ToArrayAsync(cancellationToken);
        return ToLineage(petId, relationships);
    }

    public async Task AddAsync(
        PetLineage lineage,
        CancellationToken cancellationToken = default)
    {
        await SynchronizeAsync(lineage, cancellationToken);
    }

    public Task UpdateAsync(
        PetLineage lineage,
        CancellationToken cancellationToken = default)
    {
        return SynchronizeAsync(lineage, cancellationToken);
    }

    public async Task<IReadOnlyList<PetLineage>> GetByPetIdsAsync(
        IEnumerable<Guid> petIds,
        CancellationToken cancellationToken = default)
    {
        var ids = petIds as ICollection<Guid> ?? petIds.ToArray();
        if (ids.Count == 0)
            return Array.Empty<PetLineage>();

        var relationships = await ActiveRelationships().AsNoTracking()
            .Where(item => ids.Contains(item.ChildPetId)).ToArrayAsync(cancellationToken);
        return relationships.GroupBy(item => item.ChildPetId)
            .Select(group => ToLineage(group.Key, group.ToArray())!)
            .ToArray();
    }

    public async Task<IReadOnlyList<PetLineage>> GetChildrenByParentIdsAsync(
        IEnumerable<Guid> parentIds,
        CancellationToken cancellationToken = default)
    {
        var ids = parentIds as ICollection<Guid> ?? parentIds.ToArray();
        if (ids.Count == 0)
            return Array.Empty<PetLineage>();

        var childIds = await ActiveRelationships().AsNoTracking()
            .Where(item => ids.Contains(item.ParentPetId))
            .Select(item => item.ChildPetId).Distinct().ToArrayAsync(cancellationToken);
        var relationships = await ActiveRelationships().AsNoTracking()
            .Where(item => childIds.Contains(item.ChildPetId)).ToArrayAsync(cancellationToken);
        return relationships.GroupBy(item => item.ChildPetId)
            .Select(group => ToLineage(group.Key, group.ToArray())!)
            .ToArray();
    }

    private IQueryable<PetRelationship> ActiveRelationships() =>
        _context.PetRelationships.Where(item =>
            item.Status == PetRelationshipStatus.Active && item.DeletedAtUtc == null);

    private static PetLineage? ToLineage(Guid petId,
        IReadOnlyCollection<PetRelationship> relationships)
    {
        if (relationships.Count == 0) return null;
        var first = relationships.OrderBy(item => item.CreatedAtUtc).First();
        var father = relationships.FirstOrDefault(item => item.ParentRole == ParentRole.Father)?.ParentPetId;
        var mother = relationships.FirstOrDefault(item => item.ParentRole == ParentRole.Mother)?.ParentPetId;
        return PetLineage.Create(petId, first.CreatedByUserId, father, mother, first.CreatedAtUtc).Value;
    }

    private async Task SynchronizeAsync(PetLineage lineage, CancellationToken cancellationToken)
    {
        var current = await ActiveRelationships()
            .Where(item => item.ChildPetId == lineage.PetId).ToArrayAsync(cancellationToken);
        await SynchronizeRoleAsync(lineage, ParentRole.Father, lineage.FatherId, current, cancellationToken);
        await SynchronizeRoleAsync(lineage, ParentRole.Mother, lineage.MotherId, current, cancellationToken);
    }

    private async Task SynchronizeRoleAsync(PetLineage lineage, ParentRole role, Guid? desiredParent,
        IReadOnlyCollection<PetRelationship> current, CancellationToken cancellationToken)
    {
        var existing = current.FirstOrDefault(item => item.ParentRole == role);
        if (existing?.ParentPetId == desiredParent) return;
        if (existing is not null) existing.SoftDelete(lineage.UpdatedAt);
        if (desiredParent.HasValue)
            await _context.PetRelationships.AddAsync(PetRelationship.CreateActive(
                lineage.PetId, desiredParent.Value, role, lineage.OwnerId, lineage.UpdatedAt),
                cancellationToken);
    }
}
