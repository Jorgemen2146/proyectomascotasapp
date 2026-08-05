using DogPlatform.Matching.Domain.Aggregates.FavoriteCandidate;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.Matching.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Matching.Infrastructure.Persistence.Repositories;

public sealed class FavoriteCandidateRepository : IFavoriteCandidateRepository
{
    private readonly MatchingDbContext _context;

    public FavoriteCandidateRepository(MatchingDbContext context)
    {
        _context = context;
    }

    public async Task<FavoriteCandidate?> GetAsync(
        Guid sourcePetId, Guid candidatePetId, CancellationToken cancellationToken = default)
    {
        return await _context.FavoriteCandidates
            .FirstOrDefaultAsync(
                f => f.SourcePetId == sourcePetId && f.CandidatePetId == candidatePetId,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid sourcePetId, Guid candidatePetId, CancellationToken cancellationToken = default)
    {
        return await _context.FavoriteCandidates
            .AsNoTracking()
            .AnyAsync(f => f.SourcePetId == sourcePetId && f.CandidatePetId == candidatePetId, cancellationToken);
    }

    public async Task<(IReadOnlyCollection<FavoriteCandidate> Items, int TotalItems)> GetPagedAsync(
        Guid sourcePetId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.FavoriteCandidates
            .AsNoTracking()
            .Where(f => f.SourcePetId == sourcePetId);

        var totalItems = await query.CountAsync(cancellationToken);

        if (totalItems == 0)
            return (Array.Empty<FavoriteCandidate>(), 0);

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public void Add(FavoriteCandidate favorite) => _context.FavoriteCandidates.Add(favorite);

    public void Remove(FavoriteCandidate favorite) => _context.FavoriteCandidates.Remove(favorite);
}
