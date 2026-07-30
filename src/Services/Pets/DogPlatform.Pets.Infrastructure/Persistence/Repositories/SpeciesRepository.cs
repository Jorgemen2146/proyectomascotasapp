using DogPlatform.Pets.Domain.Catalog;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.Pets.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Pets.Infrastructure.Persistence.Repositories;

public sealed class SpeciesRepository : ISpeciesRepository
{
    private readonly PetsDbContext _context;

    public SpeciesRepository(PetsDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Species>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var species = await _context.Species
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return species.AsReadOnly();
    }

    public async Task<bool> ExistsAsync(
        int speciesId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Species
            .AsNoTracking()
            .AnyAsync(s => s.Id == speciesId, cancellationToken);
    }
}
