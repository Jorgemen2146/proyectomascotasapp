namespace DogPlatform.Health.Domain.Entities;

public sealed class PetVaccination
{
    private PetVaccination() { }

    private PetVaccination(Guid id, Guid petId, int vaccineId, int? doseNumber,
        DateTime appliedAtUtc, DateTime? nextDueAtUtc, string? veterinarianName,
        string? clinicName, string? batchNumber, string? notes, DateTime createdAtUtc)
    {
        PetVaccinationId = id;
        PetId = petId;
        VaccineId = vaccineId;
        DoseNumber = doseNumber;
        AppliedAtUtc = appliedAtUtc;
        NextDueAtUtc = nextDueAtUtc;
        VeterinarianName = veterinarianName;
        ClinicName = clinicName;
        BatchNumber = batchNumber;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid PetVaccinationId { get; private set; }
    public Guid PetId { get; private set; }
    public int VaccineId { get; private set; }
    public int? DoseNumber { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }
    public DateTime? NextDueAtUtc { get; private set; }
    public string? VeterinarianName { get; private set; }
    public string? ClinicName { get; private set; }
    public string? BatchNumber { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }

    public static PetVaccination Create(Guid petId, int vaccineId, int? doseNumber,
        DateTime appliedAtUtc, DateTime? nextDueAtUtc, string? veterinarianName,
        string? clinicName, string? batchNumber, string? notes, DateTime utcNow) =>
        new(Guid.NewGuid(), petId, vaccineId, doseNumber, appliedAtUtc, nextDueAtUtc,
            veterinarianName, clinicName, batchNumber, notes, utcNow);

    public void Update(int? doseNumber, DateTime appliedAtUtc, DateTime? nextDueAtUtc,
        string? veterinarianName, string? clinicName, string? batchNumber,
        string? notes, DateTime utcNow)
    {
        DoseNumber = doseNumber;
        AppliedAtUtc = appliedAtUtc;
        NextDueAtUtc = nextDueAtUtc;
        VeterinarianName = veterinarianName;
        ClinicName = clinicName;
        BatchNumber = batchNumber;
        Notes = notes;
        UpdatedAtUtc = utcNow;
    }

    public void SoftDelete(DateTime utcNow)
    {
        IsDeleted = true;
        UpdatedAtUtc = utcNow;
    }
}
