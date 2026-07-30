using DogPlatform.Pets.Domain;
using DogPlatform.Pets.Domain.Aggregates.Pet;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.Pets.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Pets.Infrastructure.Persistence.Repositories;

public sealed class PetRepository : IPetRepository
{
    private readonly PetsDbContext _context;

    public PetRepository(PetsDbContext context)
    {
        _context = context;
    }

    public async Task<Pet?> GetByIdAsync(Guid petId, CancellationToken cancellationToken = default)
    {
        return await _context.Pets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == petId, cancellationToken);
    }

    public async Task<Pet?> GetByIdWithPhotosAsync(Guid petId, CancellationToken cancellationToken = default)
    {
        return await _context.Pets
            .Include("_photos")
            .FirstOrDefaultAsync(p => p.Id == petId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Pet>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var pets = await _context.Pets
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId)
            .ToListAsync(cancellationToken);

        return pets.AsReadOnly();
    }

    public async Task AddAsync(Pet pet, CancellationToken cancellationToken = default)
    {
        await _context.Pets.AddAsync(pet, cancellationToken);
    }

    public async Task UpdateAsync(Pet pet, CancellationToken cancellationToken = default)
    {
        _context.Pets.Update(pet);
        await Task.CompletedTask;
    }
}
