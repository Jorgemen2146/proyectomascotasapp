using DogPlatform.Identity.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", "auth");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.Id)
            .HasColumnName("UserRoleId")
            .ValueGeneratedNever();

        builder.Property(ur => ur.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(ur => ur.RoleId)
            .HasColumnName("RoleId")
            .IsRequired();

        builder.Property(ur => ur.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique()
            .HasDatabaseName("UQ_UserRoles_UserId_RoleId");
    }
}
