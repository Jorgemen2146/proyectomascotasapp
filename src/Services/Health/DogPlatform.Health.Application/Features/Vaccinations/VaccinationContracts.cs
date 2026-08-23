using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Health.Application.Features.Vaccinations;

public sealed record VaccineResponse(int VaccineId, int SpeciesId, string Name, string? Description, bool IsCore);

public sealed record PetVaccinationResponse(
    Guid PetVaccinationId,
    Guid PetId,
    int VaccineId,
    string VaccineName,
    int? DoseNumber,
    DateTime? AppliedAtUtc,
    DateTime? NextDueAtUtc,
    string Status,
    int? DaysRemaining,
    int? DaysOverdue,
    string? VeterinarianName,
    string? ClinicName,
    string? BatchNumber,
    string? Notes);

public sealed record CreateVaccinationResponse(
    Guid PetVaccinationId,
    DateTime? NextDueAtUtc,
    string Status,
    int? DaysRemaining,
    int? DaysOverdue);

public sealed record VaccinationStatusSummary(int UpToDate, int DueSoon, int DueToday, int Overdue, int NotStarted);

public sealed record VaccinationStatusVaccineResponse(
    Guid PetVaccinationId,
    Guid PetId,
    int VaccineId,
    string VaccineName,
    int? DoseNumber,
    DateTime? AppliedAtUtc,
    DateTime? NextDueAtUtc,
    string Status,
    int? DaysRemaining,
    int? DaysOverdue,
    string? VeterinarianName,
    string? ClinicName,
    string? BatchNumber,
    string? Notes,
    bool Eligible,
    DateTime? RecommendedDueAtUtc,
    int? DaysUntilEligible);

public sealed record VaccinationStatusResponse(
    Guid PetId,
    VaccinationStatusSummary Summary,
    IReadOnlyCollection<VaccinationStatusVaccineResponse> Vaccines);

public sealed record GetVaccinesQuery(int SpeciesId) : IRequest<Result<IReadOnlyCollection<VaccineResponse>>>;
public sealed record GetPetVaccinationsQuery(Guid PetId) : IRequest<Result<IReadOnlyCollection<PetVaccinationResponse>>>;
public sealed record GetPetVaccinationStatusQuery(Guid PetId) : IRequest<Result<VaccinationStatusResponse>>;

public sealed record CreateVaccinationCommand(
    Guid PetId, int VaccineId, int? DoseNumber, DateTime AppliedAtUtc,
    string? VeterinarianName, string? ClinicName, string? BatchNumber, string? Notes)
    : IRequest<Result<CreateVaccinationResponse>>;

public sealed record UpdateVaccinationCommand(
    Guid PetId, Guid PetVaccinationId, int? DoseNumber, DateTime AppliedAtUtc,
    string? VeterinarianName, string? ClinicName, string? BatchNumber, string? Notes)
    : IRequest<Result<PetVaccinationResponse>>;

public sealed record DeleteVaccinationCommand(Guid PetId, Guid PetVaccinationId) : IRequest<Result>;
