using DogPlatform.Matching.Domain.Aggregates.PetMatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Matching.Infrastructure.Persistence.Configurations;

public sealed class PetMatchConfiguration : IEntityTypeConfiguration<PetMatch>
{
    public void Configure(EntityTypeBuilder<PetMatch> builder)
    {
        builder.ToTable("PetMatches", "matching");
        builder.HasKey(match => match.Id);
        builder.Property(match => match.Id).HasColumnName("MatchId").ValueGeneratedNever();
        builder.Property(match => match.MatchRequestId).IsRequired();
        builder.Property(match => match.Pet1Id).IsRequired();
        builder.Property(match => match.Pet2Id).IsRequired();
        builder.Property(match => match.Owner1Id).IsRequired();
        builder.Property(match => match.Owner2Id).IsRequired();
        builder.Property(match => match.Owner1ShareDisplayName).IsRequired();
        builder.Property(match => match.Owner1SharePhoneNumber).IsRequired();
        builder.Property(match => match.Owner2ShareDisplayName).IsRequired();
        builder.Property(match => match.Owner2SharePhoneNumber).IsRequired();
        builder.Property(match => match.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(match => match.CreatedAtUtc).IsRequired();
        builder.HasIndex(match => match.MatchRequestId).IsUnique();
        builder.HasIndex(match => new { match.Pet1Id, match.Pet2Id }).IsUnique();
        builder.HasIndex(match => new { match.Owner1Id, match.Status });
        builder.HasIndex(match => new { match.Owner2Id, match.Status });
        builder.HasOne<Domain.Aggregates.MatchRequest.MatchRequest>().WithOne()
            .HasForeignKey<PetMatch>(match => match.MatchRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
