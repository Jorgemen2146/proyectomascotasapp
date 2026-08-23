namespace DogPlatform.Health.Domain.Entities;

public sealed class VaccineSchedule
{
    private VaccineSchedule() { }

    public VaccineSchedule(int vaccineScheduleId, int vaccineId, int doseNumber,
        int? minAgeWeeks, int? intervalDays, int? boosterIntervalDays,
        bool isActive, DateTime createdAt, DateTime? updatedAt = null)
    {
        VaccineScheduleId = vaccineScheduleId;
        VaccineId = vaccineId;
        DoseNumber = doseNumber;
        MinAgeWeeks = minAgeWeeks;
        IntervalDays = intervalDays;
        BoosterIntervalDays = boosterIntervalDays;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public int VaccineScheduleId { get; private set; }
    public int VaccineId { get; private set; }
    public int DoseNumber { get; private set; }
    public int? MinAgeWeeks { get; private set; }
    public int? IntervalDays { get; private set; }
    public int? BoosterIntervalDays { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}
