using DogPlatform.Identity.Domain.Aggregates.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "auth");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("RoleId")
            .ValueGeneratedNever();

        builder.Property(r => r.Name)
            .HasColumnName("Name")
            .HasMaxLength(Role.NameMaxLength)
            .IsRequired();

        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("UQ_Roles_Name");

        builder.Property(r => r.Description)
            .HasColumnName("Description")
            .HasMaxLength(Role.DescriptionMaxLength);
    }
}
