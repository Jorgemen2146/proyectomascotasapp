using DogPlatform.Genealogy.Domain.Relationships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Genealogy.Infrastructure.Persistence.Configurations;

public sealed class PetRelationshipConfiguration : IEntityTypeConfiguration<PetRelationship>
{
    public void Configure(EntityTypeBuilder<PetRelationship> builder)
    {
        builder.ToTable("PetRelationships", "genealogy");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("RelationshipId").ValueGeneratedNever();
        builder.Property(item => item.ParentRole).HasConversion<string>().HasMaxLength(20);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(item => item.CreatedAtUtc).HasPrecision(7);
        builder.Property(item => item.ActivatedAtUtc).HasPrecision(7);
        builder.Property(item => item.DeletedAtUtc).HasPrecision(7);
        builder.Ignore(item => item.IsActive);
        builder.HasIndex(item => item.ChildPetId).HasDatabaseName("IX_PetRelationships_ChildPetId");
        builder.HasIndex(item => item.ParentPetId).HasDatabaseName("IX_PetRelationships_ParentPetId");
        builder.HasIndex(item => item.Status).HasDatabaseName("IX_PetRelationships_Status");
        builder.HasIndex(item => new { item.ChildPetId, item.ParentRole })
            .IsUnique().HasFilter("[Status] = 'Active' AND [DeletedAtUtc] IS NULL")
            .HasDatabaseName("UX_PetRelationships_ActiveChildRole");
        builder.HasIndex(item => new { item.ChildPetId, item.ParentPetId, item.ParentRole })
            .IsUnique().HasFilter("[DeletedAtUtc] IS NULL")
            .HasDatabaseName("UX_PetRelationships_CurrentPairRole");
    }
}

public sealed class RelationshipInvitationConfiguration : IEntityTypeConfiguration<RelationshipInvitation>
{
    public void Configure(EntityTypeBuilder<RelationshipInvitation> builder)
    {
        builder.ToTable("RelationshipInvitations", "genealogy");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("InvitationId").ValueGeneratedNever();
        builder.Property(item => item.ParentRole).HasConversion<string>().HasMaxLength(20);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(item => item.RequesterDisplayName).HasMaxLength(200);
        builder.Property(item => item.TargetEmail).HasMaxLength(320);
        builder.Property(item => item.TokenHash).HasColumnType("char(64)").HasMaxLength(64);
        builder.Property(item => item.ExpiresAtUtc).HasPrecision(7);
        builder.Property(item => item.CreatedAtUtc).HasPrecision(7);
        builder.Property(item => item.AcceptedAtUtc).HasPrecision(7);
        builder.Property(item => item.RejectedAtUtc).HasPrecision(7);
        builder.Property(item => item.CancelledAtUtc).HasPrecision(7);
        builder.HasIndex(item => item.TokenHash).IsUnique()
            .HasDatabaseName("UX_RelationshipInvitations_TokenHash");
        builder.HasIndex(item => item.RequesterUserId)
            .HasDatabaseName("IX_RelationshipInvitations_RequesterUserId");
        builder.HasIndex(item => item.TargetUserId)
            .HasDatabaseName("IX_RelationshipInvitations_TargetUserId");
        builder.HasIndex(item => item.Status).HasDatabaseName("IX_RelationshipInvitations_Status");
        builder.HasIndex(item => item.ExpiresAtUtc)
            .HasDatabaseName("IX_RelationshipInvitations_ExpiresAtUtc");
        builder.HasIndex(item => new { item.ChildPetId, item.ParentRole, item.TargetEmail })
            .IsUnique().HasFilter("[Status] = 'Pending'")
            .HasDatabaseName("UX_RelationshipInvitations_PendingEquivalent");
    }
}
