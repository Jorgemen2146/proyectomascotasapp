using DogPlatform.Health.Domain.Entities;
using DogPlatform.Health.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Health.Infrastructure.Persistence;

public sealed class HealthDbContext : DbContext, IHealthUnitOfWork
{
    public HealthDbContext(DbContextOptions<HealthDbContext> options) : base(options) { }
    public DbSet<Vaccine> Vaccines => Set<Vaccine>();
    public DbSet<VaccineSchedule> VaccineSchedules => Set<VaccineSchedule>();
    public DbSet<PetVaccination> PetVaccinations => Set<PetVaccination>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HealthDbContext).Assembly);

    async Task IHealthUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await base.SaveChangesAsync(cancellationToken);
}
