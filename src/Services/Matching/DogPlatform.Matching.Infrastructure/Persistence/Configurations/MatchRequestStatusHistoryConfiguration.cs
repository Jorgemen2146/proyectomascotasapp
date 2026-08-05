using DogPlatform.Matching.Domain.Aggregates.MatchRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Matching.Infrastructure.Persistence.Configurations;

public sealed class MatchRequestStatusHistoryConfiguration : IEntityTypeConfiguration<MatchRequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<MatchRequestStatusHistory> builder)
    {
        builder.ToTable("MatchRequestStatusHistory", "matching");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("MatchRequestStatusHistoryId").ValueGeneratedNever();

        builder.Property(h => h.MatchRequestId).IsRequired();
        builder.Property(h => h.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.OccurredAt).IsRequired();
    }
}
