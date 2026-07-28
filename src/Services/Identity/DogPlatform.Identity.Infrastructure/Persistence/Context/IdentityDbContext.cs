using DogPlatform.Identity.Application;
using DogPlatform.Identity.Domain.Aggregates.RefreshToken;
using DogPlatform.Identity.Domain.Aggregates.Role;
using DogPlatform.Identity.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Identity.Infrastructure.Persistence.Context;

public sealed class IdentityDbContext : DbContext, IIdentityUnitOfWork
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auth");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
