using DogPlatform.Health.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DogPlatform.Health.Infrastructure.Persistence;

public sealed class VaccineConfiguration : IEntityTypeConfiguration<Vaccine>
{
    public void Configure(EntityTypeBuilder<Vaccine> builder)
    {
        builder.ToTable("Vaccines", "health");
        builder.HasKey(x => x.VaccineId);
        builder.Property(x => x.VaccineId).ValueGeneratedOnAdd();
        builder.Property(x => x.SpeciesId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        builder.HasIndex(x => new { x.SpeciesId, x.IsActive }).HasDatabaseName("IX_Vaccines_SpeciesId_IsActive");
        builder.HasIndex(x => new { x.SpeciesId, x.Name }).IsUnique().HasDatabaseName("UX_Vaccines_SpeciesId_Name");
    }
}

public sealed class VaccineScheduleConfiguration : IEntityTypeConfiguration<VaccineSchedule>
{
    public void Configure(EntityTypeBuilder<VaccineSchedule> builder)
    {
        builder.ToTable("VaccineSchedules", "health");
        builder.HasKey(x => x.VaccineScheduleId);
        builder.Property(x => x.VaccineScheduleId).ValueGeneratedOnAdd();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        builder.HasOne<Vaccine>().WithMany().HasForeignKey(x => x.VaccineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VaccineId, x.DoseNumber }).IsUnique().HasDatabaseName("UX_VaccineSchedules_VaccineId_DoseNumber");
    }
}

public sealed class PetVaccinationConfiguration : IEntityTypeConfiguration<PetVaccination>
{
    public void Configure(EntityTypeBuilder<PetVaccination> builder)
    {
        builder.ToTable("PetVaccinations", "health");
        builder.HasKey(x => x.PetVaccinationId);
        builder.Property(x => x.PetVaccinationId).ValueGeneratedNever();
        builder.Property(x => x.AppliedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.NextDueAtUtc).HasColumnType("datetime2");
        builder.Property(x => x.VeterinarianName).HasMaxLength(200);
        builder.Property(x => x.ClinicName).HasMaxLength(200);
        builder.Property(x => x.BatchNumber).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2");
        builder.HasOne<Vaccine>().WithMany().HasForeignKey(x => x.VaccineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PetId, x.AppliedAtUtc }).HasDatabaseName("IX_PetVaccinations_PetId_AppliedAtUtc");
        builder.HasIndex(x => new { x.PetId, x.VaccineId }).HasDatabaseName("IX_PetVaccinations_PetId_VaccineId");
        builder.HasIndex(x => x.NextDueAtUtc).HasDatabaseName("IX_PetVaccinations_NextDueAtUtc");
        builder.HasIndex(x => x.IsDeleted).HasDatabaseName("IX_PetVaccinations_IsDeleted");
        builder.HasIndex(x => new { x.PetId, x.VaccineId, x.AppliedAtUtc, x.DoseNumber })
            .IsUnique().HasFilter("[IsDeleted] = 0").HasDatabaseName("UX_PetVaccinations_ExactActive");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
