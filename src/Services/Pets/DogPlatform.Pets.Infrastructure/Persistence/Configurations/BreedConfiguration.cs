using DogPlatform.Pets.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Pets.Infrastructure.Persistence.Configurations;

public sealed class BreedConfiguration : IEntityTypeConfiguration<Breed>
{
    public void Configure(EntityTypeBuilder<Breed> builder)
    {
        builder.ToTable("Breeds", "catalog");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("BreedId");

        builder.Property(b => b.SpeciesId)
            .HasColumnName("SpeciesId")
            .IsRequired();

        builder.Property(b => b.Name)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne<Species>()
            .WithMany()
            .HasForeignKey(b => b.SpeciesId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
