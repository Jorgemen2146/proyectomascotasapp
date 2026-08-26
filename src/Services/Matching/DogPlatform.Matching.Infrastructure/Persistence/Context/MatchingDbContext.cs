using DogPlatform.Matching.Domain.Aggregates.FavoriteCandidate;
using DogPlatform.Matching.Domain.Aggregates.MatchingProfile;
using DogPlatform.Matching.Domain.Aggregates.MatchRequest;
using DogPlatform.Matching.Domain.Aggregates.PetMatch;
using DogPlatform.Matching.Domain.Aggregates.BreedingIntent;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.Matching.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using DogPlatform.Matching.Domain.Errors;

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
    public DbSet<PetMatch> PetMatches { get; set; } = null!;
    public DbSet<BreedingIntent> BreedingIntents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new MatchingProfileConfiguration());
        modelBuilder.ApplyConfiguration(new MatchingProfileBreedPreferenceConfiguration());
        modelBuilder.ApplyConfiguration(new FavoriteCandidateConfiguration());
        modelBuilder.ApplyConfiguration(new MatchRequestConfiguration());
        modelBuilder.ApplyConfiguration(new MatchRequestStatusHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new PetMatchConfiguration());
        modelBuilder.ApplyConfiguration(new BreedingIntentConfiguration());
    }

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.Entries.Any(entry => entry.Entity is BreedingIntent)
            && exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new BreedingIntentConflictException(exception);
        }
    }
}
