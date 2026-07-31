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
}
