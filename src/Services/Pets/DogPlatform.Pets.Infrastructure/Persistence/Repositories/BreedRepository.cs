using DogPlatform.Pets.Domain.Catalog;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.Pets.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Pets.Infrastructure.Persistence.Repositories;

public sealed class BreedRepository : IBreedRepository
{
    private readonly PetsDbContext _context;

    public BreedRepository(PetsDbContext context)
    {
        _context = context;
    }

    public async Task<Breed?> GetByIdAsync(int breedId, CancellationToken cancellationToken = default)
    {
        return await _context.Breeds
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == breedId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Breed>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var breeds = await _context.Breeds
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        return breeds.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Breed>> GetBySpeciesIdAsync(
        int speciesId,
        CancellationToken cancellationToken = default)
    {
        var breeds = await _context.Breeds
            .AsNoTracking()
            .Where(b => b.SpeciesId == speciesId)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        return breeds.AsReadOnly();
    }
}
