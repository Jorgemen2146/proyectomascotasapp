using DogPlatform.Matching.Domain.Aggregates.MatchingProfile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Matching.Infrastructure.Persistence.Configurations;

public sealed class MatchingProfileBreedPreferenceConfiguration
    : IEntityTypeConfiguration<MatchingProfileBreedPreference>
{
    public void Configure(EntityTypeBuilder<MatchingProfileBreedPreference> builder)
    {
        builder.ToTable("MatchingProfileBreedPreferences", "matching");

        builder.HasKey(bp => bp.Id);
        builder.Property(bp => bp.Id).HasColumnName("MatchingProfileBreedPreferenceId").ValueGeneratedNever();

        builder.Property(bp => bp.MatchingProfileId).IsRequired();
        builder.Property(bp => bp.BreedId).IsRequired();

        builder.HasIndex(bp => new { bp.MatchingProfileId, bp.BreedId }).IsUnique();
    }
}
