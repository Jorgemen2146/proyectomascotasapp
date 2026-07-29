using DogPlatform.Pets.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Pets.Infrastructure.Persistence.Configurations;

public sealed class SpeciesConfiguration : IEntityTypeConfiguration<Species>
{
    public void Configure(EntityTypeBuilder<Species> builder)
    {
        builder.ToTable("Species", "catalog");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("SpeciesId");

        builder.Property(s => s.Name)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(100);
    }
}
