using DogPlatform.Health.Application.Services;
using DogPlatform.Health.Domain.Entities;

namespace DogPlatform.Health.Application.Features.Vaccinations;

internal static class VaccinationMapping
{
    public static PetVaccinationResponse ToResponse(PetVaccination vaccination, string vaccineName,
        IVaccinationStatusService statusService, DateTime utcNow)
    {
        var status = statusService.GetVaccinationStatus(vaccination.NextDueAtUtc, utcNow, true);
        return new(vaccination.PetVaccinationId, vaccination.PetId, vaccination.VaccineId,
            vaccineName, vaccination.DoseNumber, vaccination.AppliedAtUtc, vaccination.NextDueAtUtc,
            status.Status.ToString(), status.DaysRemaining, status.DaysOverdue,
            vaccination.VeterinarianName, vaccination.ClinicName, vaccination.BatchNumber, vaccination.Notes);
    }

    public static VaccinationStatusVaccineResponse ToStatusResponse(PetVaccination vaccination, string vaccineName,
        IVaccinationStatusService statusService, DateTime utcNow)
    {
        var response = ToResponse(vaccination, vaccineName, statusService, utcNow);
        return new(response.PetVaccinationId, response.PetId, response.VaccineId,
            response.VaccineName, response.DoseNumber, response.AppliedAtUtc, response.NextDueAtUtc,
            response.Status, response.DaysRemaining, response.DaysOverdue,
            response.VeterinarianName, response.ClinicName, response.BatchNumber, response.Notes,
            true, null, null);
    }
}
