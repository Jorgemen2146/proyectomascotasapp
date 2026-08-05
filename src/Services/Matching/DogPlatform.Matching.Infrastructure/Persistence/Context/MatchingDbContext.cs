using DogPlatform.Matching.Domain.Aggregates.FavoriteCandidate;
using DogPlatform.Matching.Domain.Aggregates.MatchingProfile;
using DogPlatform.Matching.Domain.Aggregates.MatchRequest;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.Matching.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Matching.Infrastructure.Persistence.Context;

public sealed class MatchingDbContext : DbContext, IMatchingUnitOfWork
{
    public MatchingDbContext(DbContextOptions<MatchingDbContext> options)
        : base(options)
    {
    }

    public DbSet<MatchingProfile> MatchingProfiles { get; set; } = null!;
    public DbSet<FavoriteCandidate> FavoriteCandidates { get; set; } = null!;
    public DbSet<MatchRequest> MatchRequests { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new MatchingProfileConfiguration());
        modelBuilder.ApplyConfiguration(new MatchingProfileBreedPreferenceConfiguration());
        modelBuilder.ApplyConfiguration(new FavoriteCandidateConfiguration());
        modelBuilder.ApplyConfiguration(new MatchRequestConfiguration());
        modelBuilder.ApplyConfiguration(new MatchRequestStatusHistoryConfiguration());
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
    }
}
