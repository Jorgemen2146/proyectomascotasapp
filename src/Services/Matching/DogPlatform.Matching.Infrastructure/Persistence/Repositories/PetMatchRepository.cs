using DogPlatform.Matching.Domain.Aggregates.PetMatch;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.Matching.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Matching.Infrastructure.Persistence.Repositories;

public sealed class PetMatchRepository(MatchingDbContext context) : IPetMatchRepository
{
    public Task<PetMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.PetMatches.AsNoTracking().FirstOrDefaultAsync(match => match.Id == id, cancellationToken);
    public Task<PetMatch?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        context.PetMatches.AsNoTracking().FirstOrDefaultAsync(match => match.MatchRequestId == requestId, cancellationToken);
    public async Task<IReadOnlyList<PetMatch>> GetByOwnerIdAsync(Guid ownerId,
        CancellationToken cancellationToken = default) =>
        await context.PetMatches.AsNoTracking()
            .Where(match => match.Owner1Id == ownerId || match.Owner2Id == ownerId)
            .OrderByDescending(match => match.CreatedAtUtc).ToListAsync(cancellationToken);
    public void Add(PetMatch match) => context.PetMatches.Add(match);
}
