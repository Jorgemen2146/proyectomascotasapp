using DogPlatform.Matching.Domain.Aggregates.BreedingIntent;
using DogPlatform.Matching.Domain.Aggregates.PetMatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Matching.Infrastructure.Persistence.Configurations;

public sealed class BreedingIntentConfiguration : IEntityTypeConfiguration<BreedingIntent>
{
    public void Configure(EntityTypeBuilder<BreedingIntent> builder)
    {
        builder.ToTable("BreedingIntents", "matching");
        builder.HasKey(intent => intent.Id);
        builder.Property(intent => intent.Id).HasColumnName("BreedingIntentId").ValueGeneratedNever();
        builder.Property(intent => intent.MatchId).IsRequired();
        builder.Property(intent => intent.OpenMatchId);
        builder.Property(intent => intent.ProposerOwnerId).IsRequired();
        builder.Property(intent => intent.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(intent => intent.Notes).HasMaxLength(1000);
        builder.Property(intent => intent.CreatedAtUtc).IsRequired();
        builder.HasIndex(intent => new { intent.MatchId, intent.Status });
        builder.HasIndex(intent => intent.OpenMatchId)
            .IsUnique()
            .HasFilter("[OpenMatchId] IS NOT NULL");
        builder.HasIndex(intent => intent.ProposerOwnerId);
        builder.HasOne<PetMatch>().WithMany().HasForeignKey(intent => intent.MatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
