using DogPlatform.Matching.Domain.Aggregates.BreedingIntent;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.Matching.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Matching.Infrastructure.Persistence.Repositories;

public sealed class BreedingIntentRepository(MatchingDbContext context) : IBreedingIntentRepository
{
    public Task<BreedingIntent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.BreedingIntents.FirstOrDefaultAsync(intent => intent.Id == id, cancellationToken);
    public Task<BreedingIntent?> GetLatestByMatchIdAsync(Guid matchId,
        CancellationToken cancellationToken = default) =>
        context.BreedingIntents.AsNoTracking()
            .Where(intent => intent.MatchId == matchId)
            .OrderByDescending(intent => intent.CreatedAtUtc)
            .ThenByDescending(intent => intent.Id)
            .FirstOrDefaultAsync(cancellationToken);
    public Task<bool> HasOpenIntentAsync(Guid matchId, CancellationToken cancellationToken = default) =>
        context.BreedingIntents.AsNoTracking()
            .AnyAsync(intent => intent.OpenMatchId == matchId, cancellationToken);
    public void Add(BreedingIntent intent) => context.BreedingIntents.Add(intent);
    public void Update(BreedingIntent intent) => context.BreedingIntents.Update(intent);
}
