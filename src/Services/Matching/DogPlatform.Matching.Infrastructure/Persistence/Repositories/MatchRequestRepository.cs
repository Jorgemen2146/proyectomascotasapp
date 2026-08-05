using DogPlatform.Matching.Domain.Aggregates.MatchRequest;
using DogPlatform.Matching.Domain.Enums;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.Matching.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Matching.Infrastructure.Persistence.Repositories;

public sealed class MatchRequestRepository : IMatchRequestRepository
{
    private readonly MatchingDbContext _context;

    public MatchRequestRepository(MatchingDbContext context)
    {
        _context = context;
    }

    public async Task<MatchRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _context.MatchRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
    }

    public async Task<bool> HasActiveRequestAsync(
        Guid requesterPetId, Guid candidatePetId, CancellationToken cancellationToken = default)
    {
        return await _context.MatchRequests
            .AsNoTracking()
            .AnyAsync(r =>
                ((r.RequesterPetId == requesterPetId && r.CandidatePetId == candidatePetId) ||
                 (r.RequesterPetId == candidatePetId && r.CandidatePetId == requesterPetId)) &&
                (r.Status == MatchRequestStatus.Pending || r.Status == MatchRequestStatus.Accepted),
                cancellationToken);
    }

    public async Task<(IReadOnlyCollection<MatchRequest> Items, int TotalItems)> GetIncomingAsync(
        Guid candidateOwnerId,
        MatchRequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MatchRequests
            .AsNoTracking()
            .Where(r => r.CandidateOwnerId == candidateOwnerId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var totalItems = await query.CountAsync(cancellationToken);

        if (totalItems == 0)
            return (Array.Empty<MatchRequest>(), 0);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public async Task<(IReadOnlyCollection<MatchRequest> Items, int TotalItems)> GetOutgoingAsync(
        Guid requesterOwnerId,
        MatchRequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MatchRequests
            .AsNoTracking()
            .Where(r => r.RequesterOwnerId == requesterOwnerId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var totalItems = await query.CountAsync(cancellationToken);

        if (totalItems == 0)
            return (Array.Empty<MatchRequest>(), 0);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public void Add(MatchRequest request) => _context.MatchRequests.Add(request);

    public void Update(MatchRequest request) => _context.MatchRequests.Update(request);
}
