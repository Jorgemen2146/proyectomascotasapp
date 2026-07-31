using DogPlatform.Pets.Domain.Aggregates.Pet;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.Pets.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Pets.Infrastructure.Persistence.Repositories;

public sealed class PetPhotoRepository : IPetPhotoRepository
{
    private readonly PetsDbContext _context;

    public PetPhotoRepository(PetsDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<PetPhoto>> GetByPetIdAsync(
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        var photos = await _context.PetPhotos
            .AsNoTracking()
            .Where(p => p.PetId == petId)
            .OrderByDescending(p => p.IsMain)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return photos.AsReadOnly();
    }

    public async Task<PetPhoto?> GetByIdAsync(
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PetPhotos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == photoId, cancellationToken);
    }

    public async Task AddAsync(PetPhoto photo, CancellationToken cancellationToken = default)
    {
        await _context.PetPhotos.AddAsync(photo, cancellationToken);
    }

    public async Task RemoveAsync(PetPhoto photo, CancellationToken cancellationToken = default)
    {
        _context.PetPhotos.Remove(photo);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsByUrlAsync(Guid petId, string url, CancellationToken cancellationToken = default)
    {
        return await _context.PetPhotos
            .AsNoTracking()
            .AnyAsync(p => p.PetId == petId && p.Url == url, cancellationToken);
    }
}
