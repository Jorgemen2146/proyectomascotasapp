using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.Genealogy.Domain.Relationships;
using DogPlatform.Genealogy.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Genealogy.Infrastructure.Persistence.Context;

public sealed class GenealogyDbContext : DbContext, IGenealogyUnitOfWork
{
    public GenealogyDbContext(DbContextOptions<GenealogyDbContext> options)
        : base(options)
    {
    }

    public DbSet<PetRelationship> PetRelationships { get; set; } = null!;
    public DbSet<RelationshipInvitation> RelationshipInvitations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new PetRelationshipConfiguration());
        modelBuilder.ApplyConfiguration(new RelationshipInvitationConfiguration());
    }

    async Task IGenealogyUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await base.SaveChangesAsync(cancellationToken);
}
