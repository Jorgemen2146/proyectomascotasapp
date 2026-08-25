using DogPlatform.Identity.Domain.Legal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserLegalConsentConfiguration : IEntityTypeConfiguration<UserLegalConsent>
{
    public void Configure(EntityTypeBuilder<UserLegalConsent> builder)
    {
        builder.ToTable("UserLegalConsents", "auth");
        builder.HasKey(consent => consent.Id);
        builder.Property(consent => consent.Id).HasColumnName("UserLegalConsentId").ValueGeneratedNever();
        builder.Property(consent => consent.UserId).IsRequired();
        builder.Property(consent => consent.LegalDocumentId).IsRequired();
        builder.Property(consent => consent.AcceptedAtUtc).IsRequired();
        builder.HasIndex(consent => new { consent.UserId, consent.LegalDocumentId })
            .IsUnique().HasDatabaseName("UX_UserLegalConsents_UserId_LegalDocumentId");
        builder.HasIndex(consent => consent.UserId).HasDatabaseName("IX_UserLegalConsents_UserId");
        builder.HasIndex(consent => consent.AcceptedAtUtc).HasDatabaseName("IX_UserLegalConsents_AcceptedAtUtc");
        builder.HasOne<Domain.Aggregates.User.User>().WithMany()
            .HasForeignKey(consent => consent.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LegalDocument>().WithMany()
            .HasForeignKey(consent => consent.LegalDocumentId).OnDelete(DeleteBehavior.Restrict);
    }
}
