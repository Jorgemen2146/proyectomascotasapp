using DogPlatform.Pets.Domain.Aggregates.Pet;
using DogPlatform.Pets.Domain.Catalog;
using DogPlatform.Pets.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Pets.Infrastructure.Persistence.Context;

public sealed class PetsDbContext : DbContext, IPetsUnitOfWork
{
    public PetsDbContext(DbContextOptions<PetsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Pet> Pets { get; set; } = null!;
    public DbSet<Breed> Breeds { get; set; } = null!;
    public DbSet<Species> Species { get; set; } = null!;
    public DbSet<PetPhoto> PetPhotos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new PetConfiguration());
        modelBuilder.ApplyConfiguration(new PetPhotoConfiguration());
        modelBuilder.ApplyConfiguration(new BreedConfiguration());
        modelBuilder.ApplyConfiguration(new SpeciesConfiguration());
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
    }
}
