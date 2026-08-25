using DogPlatform.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Notification.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options)
{
    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<NotificationRecord>();
        builder.ToTable("Notifications", "notifications");
        builder.HasKey(x => x.NotificationId);
        builder.Property(x => x.NotificationId).ValueGeneratedNever();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ReferenceType).HasMaxLength(100);
        builder.Property(x => x.ReferenceId).HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReadAtUtc).HasColumnType("datetime2");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.NotificationDateUtc).HasColumnType("date").IsRequired();
        builder.Property(x => x.DeduplicationKey).HasMaxLength(300).IsRequired();
        builder.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Notifications_UserId_CreatedAtUtc");
        builder.HasIndex(x => new { x.UserId, x.IsRead })
            .HasDatabaseName("IX_Notifications_UserId_IsRead");
        builder.HasIndex(x => x.NotificationDateUtc)
            .HasDatabaseName("IX_Notifications_NotificationDateUtc");
        builder.HasIndex(x => x.Type).HasDatabaseName("IX_Notifications_Type");
        builder.HasIndex(x => x.PetId).HasDatabaseName("IX_Notifications_PetId");
        builder.HasIndex(x => x.DeduplicationKey).IsUnique()
            .HasDatabaseName("UX_Notifications_DeduplicationKey");
    }
}
