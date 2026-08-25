using DogPlatform.Identity.Domain.Legal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Identity.Infrastructure.Persistence.Configurations;

internal sealed class LegalDocumentConfiguration : IEntityTypeConfiguration<LegalDocument>
{
    public void Configure(EntityTypeBuilder<LegalDocument> builder)
    {
        builder.ToTable("LegalDocuments", "auth");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.Id).HasColumnName("LegalDocumentId").ValueGeneratedNever();
        builder.Property(document => document.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(document => document.Version).HasMaxLength(30).IsRequired();
        builder.Property(document => document.Title).HasMaxLength(200).IsRequired();
        builder.Property(document => document.Content).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(document => document.PublishedAtUtc).IsRequired();
        builder.Property(document => document.EffectiveAtUtc).IsRequired();
        builder.Property(document => document.IsActive).IsRequired();
        builder.Property(document => document.RequiresAcceptance).IsRequired();
        builder.Property(document => document.CreatedAtUtc).IsRequired();
        builder.HasIndex(document => new { document.Type, document.Version })
            .IsUnique().HasDatabaseName("UX_LegalDocuments_Type_Version");
        builder.HasIndex(document => document.IsActive).HasDatabaseName("IX_LegalDocuments_IsActive");
        builder.HasIndex(document => document.Type).HasDatabaseName("IX_LegalDocuments_Type");
    }
}
