using DogPlatform.Matching.Domain.Aggregates.MatchingProfile;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.Matching.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Matching.Infrastructure.Persistence.Repositories;

public sealed class MatchingProfileRepository : IMatchingProfileRepository
{
    private readonly MatchingDbContext _context;

    public MatchingProfileRepository(MatchingDbContext context)
    {
        _context = context;
    }

    public async Task<MatchingProfile?> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken = default)
    {
        return await _context.MatchingProfiles
            .Include(p => p.BreedPreferences)
            .FirstOrDefaultAsync(p => p.PetId == petId, cancellationToken);
    }

    public async Task<MatchingProfile?> GetActiveByPetIdAsync(Guid petId, CancellationToken cancellationToken = default)
    {
        return await _context.MatchingProfiles
            .AsNoTracking()
            .Include(p => p.BreedPreferences)
            .FirstOrDefaultAsync(p => p.PetId == petId && p.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<MatchingProfile>> GetActiveByPetIdsAsync(
        IEnumerable<Guid> petIds, CancellationToken cancellationToken = default)
    {
        var ids = petIds.Distinct().ToArray();
        return await _context.MatchingProfiles.AsNoTracking()
            .Include(p => p.BreedPreferences)
            .Where(p => ids.Contains(p.PetId) && p.IsActive)
            .ToListAsync(cancellationToken);
    }

    public Task<MatchingProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.MatchingProfiles.Include(p => p.BreedPreferences)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(MatchingProfile profile) => _context.MatchingProfiles.Add(profile);

    public void Update(MatchingProfile profile) => _context.MatchingProfiles.Update(profile);
}
