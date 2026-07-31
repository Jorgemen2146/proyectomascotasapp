using DogPlatform.Genealogy.Domain.Aggregates.PetLineage;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.Genealogy.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Genealogy.Infrastructure.Persistence.Context;

public sealed class GenealogyDbContext : DbContext, IGenealogyUnitOfWork
{
    public GenealogyDbContext(DbContextOptions<GenealogyDbContext> options)
        : base(options)
    {
    }

    public DbSet<PetLineage> PetLineages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new PetLineageConfiguration());
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
    }
}
