using DogPlatform.Pets.Domain.Aggregates.Pet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Pets.Infrastructure.Persistence.Configurations;

public sealed class PetPhotoConfiguration : IEntityTypeConfiguration<PetPhoto>
{
    public void Configure(EntityTypeBuilder<PetPhoto> builder)
    {
        builder.ToTable("PetPhotos", "pets");

        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.Id)
            .HasColumnName("PetPhotoId");

        builder.Property(pp => pp.PetId)
            .HasColumnName("PetId")
            .IsRequired();

        builder.Property(pp => pp.Url)
            .HasColumnName("Url")
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(pp => pp.IsMain)
            .HasColumnName("IsMain")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pp => pp.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();
    }
}
