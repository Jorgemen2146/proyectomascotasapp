using DogPlatform.Identity.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "auth");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("UserId")
            .ValueGeneratedNever();

        builder.OwnsOne(u => u.FullName, fn =>
        {
            fn.Property(f => f.FirstName)
                .HasColumnName("FirstName")
                .HasMaxLength(100)
                .IsRequired();

            fn.Property(f => f.LastName)
                .HasColumnName("LastName")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.OwnsOne(u => u.Email, e =>
        {
            e.Property(em => em.Value)
                .HasColumnName("Email")
                .HasMaxLength(200)
                .IsRequired();

            e.HasIndex(em => em.Value)
                .IsUnique()
                .HasDatabaseName("UQ_Users_Email");
        });

        builder.Property(u => u.PasswordHash)
            .HasColumnName("PasswordHash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.PasswordSalt)
            .HasColumnName("PasswordSalt")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.PhoneNumber)
            .HasColumnName("PhoneNumber")
            .HasMaxLength(20);

        builder.Property(u => u.ProfilePhotoUrl)
            .HasColumnName("ProfilePhotoUrl")
            .HasMaxLength(500);

        builder.Property(u => u.IsEmailConfirmed)
            .HasColumnName("IsEmailConfirmed")
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

        builder.Property(u => u.LastLogin)
            .HasColumnName("LastLogin");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("UpdatedAt");

        builder.HasMany(u => u.UserRoles)
            .WithOne()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
