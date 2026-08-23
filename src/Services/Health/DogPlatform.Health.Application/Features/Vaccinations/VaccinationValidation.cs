using DogPlatform.Health.Domain.Errors;
using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Health.Application.Features.Vaccinations;

internal static class VaccinationValidation
{
    public static Error? Validate(Guid petId, int? doseNumber, DateTime appliedAtUtc, DateTime utcNow,
        string? veterinarianName, string? clinicName, string? batchNumber, string? notes)
    {
        if (petId == Guid.Empty) return VaccinationErrors.InvalidPetId;
        if (doseNumber is <= 0) return VaccinationErrors.InvalidDoseNumber;
        if (appliedAtUtc == default || appliedAtUtc.Kind != DateTimeKind.Utc) return VaccinationErrors.InvalidAppliedAt;
        if (appliedAtUtc > utcNow.AddMinutes(5)) return VaccinationErrors.AppliedAtTooFarInFuture;
        if (TooLong(veterinarianName, 200) || TooLong(clinicName, 200) ||
            TooLong(batchNumber, 100) || TooLong(notes, 1000)) return VaccinationErrors.InvalidFieldLength;
        return null;
    }

    private static bool TooLong(string? value, int maximum) => value?.Length > maximum;
}
