using DogPlatform.Health.Domain.Entities;
using DogPlatform.Health.Domain.Enums;

namespace DogPlatform.Health.Application.Services;

public interface IVaccinationScheduleService
{
    DateTime? CalculateNextDueDate(DateTime appliedAtUtc, int? doseNumber,
        IReadOnlyCollection<VaccineSchedule> schedules);
}

public sealed class VaccinationScheduleService : IVaccinationScheduleService
{
    public DateTime? CalculateNextDueDate(DateTime appliedAtUtc, int? doseNumber,
        IReadOnlyCollection<VaccineSchedule> schedules)
    {
        if (!doseNumber.HasValue)
            return null;

        var active = schedules.Where(x => x.IsActive).OrderBy(x => x.DoseNumber).ToArray();
        var nextDose = active.FirstOrDefault(x => x.DoseNumber == doseNumber.Value + 1);
        if (nextDose?.IntervalDays is > 0)
            return appliedAtUtc.AddDays(nextDose.IntervalDays.Value);

        var currentDose = active.FirstOrDefault(x => x.DoseNumber == doseNumber.Value);
        if (currentDose?.BoosterIntervalDays is > 0 && active.All(x => x.DoseNumber <= doseNumber.Value))
            return appliedAtUtc.AddDays(currentDose.BoosterIntervalDays.Value);

        return null;
    }
}

public sealed record VaccinationStatusResult(
    VaccinationStatus Status,
    int? DaysRemaining,
    int? DaysOverdue);

public interface IVaccinationStatusService
{
    VaccinationStatusResult GetVaccinationStatus(DateTime? nextDueAtUtc, DateTime nowUtc, bool hasStarted);
}

public sealed class VaccinationStatusService : IVaccinationStatusService
{
    public VaccinationStatusResult GetVaccinationStatus(DateTime? nextDueAtUtc, DateTime nowUtc, bool hasStarted)
    {
        if (!nextDueAtUtc.HasValue)
            return new(hasStarted ? VaccinationStatus.UpToDate : VaccinationStatus.NotStarted, null, null);

        var days = (nextDueAtUtc.Value.Date - nowUtc.Date).Days;
        if (days < 0)
            return new(VaccinationStatus.Overdue, null, -days);
        if (days == 0)
            return new(VaccinationStatus.DueToday, 0, null);
        if (days <= 7)
            return new(VaccinationStatus.DueSoon, days, null);
        return new(VaccinationStatus.UpToDate, days, null);
    }
}
