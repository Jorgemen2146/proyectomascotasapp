using DogPlatform.Genealogy.Domain.Aggregates.PetLineage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Genealogy.Infrastructure.Persistence.Configurations;

public sealed class PetLineageConfiguration : IEntityTypeConfiguration<PetLineage>
{
    public void Configure(EntityTypeBuilder<PetLineage> builder)
    {
        builder.ToTable("PetLineages", "genealogy");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("LineageId")
            .ValueGeneratedNever();

        builder.Property(l => l.PetId)
            .HasColumnName("PetId")
            .IsRequired();

        builder.Property(l => l.OwnerId)
            .HasColumnName("OwnerId")
            .IsRequired();

        builder.Property(l => l.FatherId)
            .HasColumnName("FatherId");

        builder.Property(l => l.MotherId)
            .HasColumnName("MotherId");

        builder.Property(l => l.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        // Each pet can have at most one lineage record.
        builder.HasIndex(l => l.PetId)
            .IsUnique()
            .HasDatabaseName("UX_PetLineages_PetId");
    }
}
