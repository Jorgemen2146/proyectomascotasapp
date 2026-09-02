using DogPlatform.Identity.Domain.Aggregates.ExternalLogin;
using DogPlatform.Identity.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Identity.Infrastructure.Persistence.Configurations;

internal sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("ExternalLogins", "auth");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("ExternalLoginId").ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Provider).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ProviderUserId).HasMaxLength(255).IsRequired();
        builder.Property(x => x.EmailAtLinkTime).HasMaxLength(200);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc);
        builder.HasIndex(x => new { x.Provider, x.ProviderUserId })
            .IsUnique().HasDatabaseName("UX_ExternalLogins_Provider_ProviderUserId");
        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_ExternalLogins_UserId");
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
