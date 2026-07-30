using DogPlatform.Pets.Domain.Aggregates.Pet;
using DogPlatform.Pets.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Pets.Infrastructure.Persistence.Configurations;

public sealed class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("Pets", "pets");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("PetId");

        builder.Property(p => p.OwnerId)
            .HasColumnName("OwnerId")
            .IsRequired();

        builder.Property(p => p.BreedId)
            .HasColumnName("BreedId")
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.BirthDate)
            .HasColumnName("BirthDate");

        builder.Property(p => p.Gender)
            .HasConversion(g => g.Value, v => Domain.ValueObjects.Gender.Create(v).Value)
            .HasColumnName("Gender")
            .IsRequired()
            .HasMaxLength(1);

        builder.Property(p => p.Weight)
            .HasColumnName("Weight")
            .HasPrecision(10, 2);

        builder.Property(p => p.Color)
            .HasColumnName("Color")
            .HasMaxLength(100);

        builder.Property(p => p.PedigreeNumber)
            .HasColumnName("PedigreeNumber")
            .HasMaxLength(100);

        builder.Property(p => p.IsSterilized)
            .HasColumnName("IsSterilized")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.Description)
            .HasColumnName("Description")
            .HasMaxLength(1000);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("UpdatedAt");

        builder.Property(p => p.IsDeleted)
            .HasColumnName("IsDeleted")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.DeletedAt)
            .HasColumnName("DeletedAt");

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasOne<Breed>()
            .WithMany()
            .HasForeignKey(p => p.BreedId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany<PetPhoto>()
            .WithOne()
            .HasForeignKey(pp => pp.PetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
