using DogPlatform.Matching.Domain.Aggregates.MatchingProfile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Matching.Infrastructure.Persistence.Configurations;

public sealed class MatchingProfileConfiguration : IEntityTypeConfiguration<MatchingProfile>
{
    public void Configure(EntityTypeBuilder<MatchingProfile> builder)
    {
        builder.ToTable("MatchingProfiles", "matching", tb =>
        {
            tb.HasCheckConstraint(
                "CK_MatchingProfiles_AgeRange",
                "[MinimumAgeMonths] <= [MaximumAgeMonths]");
            tb.HasCheckConstraint(
                "CK_MatchingProfiles_InbreedingCoefficient",
                "[MaximumEstimatedInbreedingCoefficient] >= 0 AND [MaximumEstimatedInbreedingCoefficient] <= 1");
            tb.HasCheckConstraint(
                "CK_MatchingProfiles_Score",
                "[MinimumCompatibilityScore] >= 0 AND [MinimumCompatibilityScore] <= 100");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("MatchingProfileId").ValueGeneratedNever();

        builder.Property(p => p.PetId).IsRequired();
        builder.Property(p => p.OwnerId).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.MinimumAgeMonths).IsRequired();
        builder.Property(p => p.MaximumAgeMonths).IsRequired();
        builder.Property(p => p.RequirePedigree).IsRequired();
        builder.Property(p => p.RequireGenealogyValidation).IsRequired();
        builder.Property(p => p.MaximumEstimatedInbreedingCoefficient).IsRequired();
        builder.Property(p => p.MinimumCompatibilityScore).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt);

        builder.HasIndex(p => p.PetId).IsUnique();
        builder.HasIndex(p => new { p.OwnerId, p.IsActive });

        builder.Metadata
            .FindNavigation(nameof(MatchingProfile.BreedPreferences))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.BreedPreferences)
            .WithOne()
            .HasForeignKey(bp => bp.MatchingProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
