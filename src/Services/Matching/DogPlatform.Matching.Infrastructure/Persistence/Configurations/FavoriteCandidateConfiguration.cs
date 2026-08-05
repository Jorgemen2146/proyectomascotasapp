using DogPlatform.Matching.Domain.Aggregates.FavoriteCandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Matching.Infrastructure.Persistence.Configurations;

public sealed class FavoriteCandidateConfiguration : IEntityTypeConfiguration<FavoriteCandidate>
{
    public void Configure(EntityTypeBuilder<FavoriteCandidate> builder)
    {
        builder.ToTable("FavoriteCandidates", "matching");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("FavoriteCandidateId").ValueGeneratedNever();

        builder.Property(f => f.SourcePetId).IsRequired();
        builder.Property(f => f.SourceOwnerId).IsRequired();
        builder.Property(f => f.CandidatePetId).IsRequired();
        builder.Property(f => f.CreatedAt).IsRequired();

        builder.HasIndex(f => new { f.SourcePetId, f.CandidatePetId }).IsUnique();
    }
}
