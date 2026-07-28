using DogPlatform.Identity.Domain.Aggregates.RefreshToken;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", "auth");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id)
            .HasColumnName("RefreshTokenId")
            .ValueGeneratedNever();

        builder.Property(rt => rt.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(rt => rt.Token)
            .HasColumnName("Token")
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("UX_RefreshTokens_Token");

        builder.Property(rt => rt.ExpiresAt)
            .HasColumnName("ExpiresAt")
            .IsRequired();

        builder.Property(rt => rt.RevokedAt)
            .HasColumnName("RevokedAt");

        builder.Property(rt => rt.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");
    }
}
