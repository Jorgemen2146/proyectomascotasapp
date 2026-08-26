using DogPlatform.Identity.Domain.Aggregates.PasswordResetCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Identity.Infrastructure.Persistence.Configurations;

internal sealed class PasswordResetCodeConfiguration : IEntityTypeConfiguration<PasswordResetCode>
{
    public void Configure(EntityTypeBuilder<PasswordResetCode> builder)
    {
        builder.ToTable("PasswordResetCodes", "auth");
        builder.HasKey(code => code.Id);
        builder.Property(code => code.Id).HasColumnName("PasswordResetCodeId").ValueGeneratedNever();
        builder.Property(code => code.UserId).IsRequired();
        builder.Property(code => code.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(code => code.CreatedAtUtc).IsRequired();
        builder.Property(code => code.ExpiresAtUtc).IsRequired();
        builder.Property(code => code.UsedAtUtc);
        builder.Property(code => code.FailedAttempts).IsRequired();
        builder.Property(code => code.IsRevoked).IsRequired();
        builder.Property(code => code.CreatedFromIp).HasMaxLength(45);

        builder.HasIndex(code => code.UserId).HasDatabaseName("IX_PasswordResetCodes_UserId");
        builder.HasIndex(code => code.ExpiresAtUtc).HasDatabaseName("IX_PasswordResetCodes_ExpiresAtUtc");
        builder.HasIndex(code => code.UsedAtUtc).HasDatabaseName("IX_PasswordResetCodes_UsedAtUtc");
    }
}
