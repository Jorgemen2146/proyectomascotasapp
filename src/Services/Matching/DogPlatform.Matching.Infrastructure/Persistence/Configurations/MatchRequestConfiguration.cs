using DogPlatform.Matching.Domain.Aggregates.MatchRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Matching.Infrastructure.Persistence.Configurations;

public sealed class MatchRequestConfiguration : IEntityTypeConfiguration<MatchRequest>
{
    public void Configure(EntityTypeBuilder<MatchRequest> builder)
    {
        builder.ToTable("MatchRequests", "matching", tb =>
        {
            tb.HasCheckConstraint(
                "CK_MatchRequests_CompatibilityScore",
                "[CompatibilityScoreSnapshot] >= 0 AND [CompatibilityScoreSnapshot] <= 100");
            tb.HasCheckConstraint(
                "CK_MatchRequests_InbreedingCoefficient",
                "[EstimatedInbreedingCoefficientSnapshot] >= 0 AND [EstimatedInbreedingCoefficientSnapshot] <= 1");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("MatchRequestId").ValueGeneratedNever();

        builder.Property(r => r.RequesterPetId).IsRequired();
        builder.Property(r => r.RequesterOwnerId).IsRequired();
        builder.Property(r => r.CandidatePetId).IsRequired();
        builder.Property(r => r.CandidateOwnerId).IsRequired();
        builder.Property(r => r.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Message).HasMaxLength(500);
        builder.Property(r => r.CompatibilityScoreSnapshot).IsRequired();
        builder.Property(r => r.EstimatedInbreedingCoefficientSnapshot).IsRequired();
        builder.Property(r => r.RelationshipTypeSnapshot).IsRequired().HasConversion<string>().HasMaxLength(40);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt);
        builder.Property(r => r.RespondedAt);
        builder.Property(r => r.CancelledAt);
        builder.Property(r => r.ExpiresAt);

        builder.HasIndex(r => new { r.RequesterPetId, r.CandidatePetId, r.Status });
        builder.HasIndex(r => new { r.RequesterOwnerId, r.Status, r.CreatedAt });
        builder.HasIndex(r => new { r.CandidateOwnerId, r.Status, r.CreatedAt });

        builder.Metadata
            .FindNavigation(nameof(MatchRequest.StatusHistory))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(r => r.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.MatchRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
