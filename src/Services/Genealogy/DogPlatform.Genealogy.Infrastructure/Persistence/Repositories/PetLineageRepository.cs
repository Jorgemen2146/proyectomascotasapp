using DogPlatform.Genealogy.Domain.Aggregates.PetLineage;
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
        return await _context.PetLineages
            .FirstOrDefaultAsync(l => l.PetId == petId, cancellationToken);
    }

    public async Task AddAsync(
        PetLineage lineage,
        CancellationToken cancellationToken = default)
    {
        await _context.PetLineages.AddAsync(lineage, cancellationToken);
    }

    public Task UpdateAsync(
        PetLineage lineage,
        CancellationToken cancellationToken = default)
    {
        _context.PetLineages.Update(lineage);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<PetLineage>> GetByPetIdsAsync(
        IEnumerable<Guid> petIds,
        CancellationToken cancellationToken = default)
    {
        var ids = petIds as ICollection<Guid> ?? petIds.ToArray();
        if (ids.Count == 0)
            return Array.Empty<PetLineage>();

        return await _context.PetLineages
            .AsNoTracking()
            .Where(l => ids.Contains(l.PetId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PetLineage>> GetChildrenByParentIdsAsync(
        IEnumerable<Guid> parentIds,
        CancellationToken cancellationToken = default)
    {
        var ids = parentIds as ICollection<Guid> ?? parentIds.ToArray();
        if (ids.Count == 0)
            return Array.Empty<PetLineage>();

        return await _context.PetLineages
            .AsNoTracking()
            .Where(l =>
                (l.FatherId != null && ids.Contains(l.FatherId.Value)) ||
                (l.MotherId != null && ids.Contains(l.MotherId.Value)))
            .ToListAsync(cancellationToken);
    }
}
