using DogPlatform.Health.Application.Services;
using DogPlatform.Health.Domain.Enums;
using DogPlatform.Health.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Health.Application.Features.Vaccinations;

public sealed record VaccinationReminderCandidateResponse(
    Guid UserId,
    Guid PetId,
    string PetName,
    int VaccineId,
    string VaccineName,
    string Status,
    bool Eligible,
    DateTime? RecommendedDueAtUtc,
    DateTime? NextDueAtUtc,
    int? DaysRemaining,
    int? DaysOverdue);

public sealed record GetVaccinationReminderCandidatesQuery(DateOnly DateUtc)
    : IRequest<Result<IReadOnlyCollection<VaccinationReminderCandidateResponse>>>;

public sealed class GetVaccinationReminderCandidatesQueryHandler(
    IInternalPetCatalogService petCatalog,
    IPetVaccinationRepository records,
    IVaccineRepository vaccines,
    IVaccinationStatusService status)
    : IRequestHandler<GetVaccinationReminderCandidatesQuery,
        Result<IReadOnlyCollection<VaccinationReminderCandidateResponse>>>
{
    public async Task<Result<IReadOnlyCollection<VaccinationReminderCandidateResponse>>> Handle(
        GetVaccinationReminderCandidatesQuery request, CancellationToken cancellationToken)
    {
        var pets = await petCatalog.GetAllAsync(cancellationToken);
        var nowUtc = DateTime.SpecifyKind(
            request.DateUtc.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var candidates = new List<VaccinationReminderCandidateResponse>();

        foreach (var pet in pets)
        {
            var petData = new PetHealthData(
                pet.PetId, pet.SpeciesId, pet.BirthDate, pet.PetName);
            var vaccinationStatus = await VaccinationStatusResponseBuilder.BuildAsync(
                petData, records, vaccines, status, nowUtc, cancellationToken);

            candidates.AddRange(vaccinationStatus.Vaccines
                .Where(IsActionable)
                .Select(vaccine => new VaccinationReminderCandidateResponse(
                    pet.UserId, pet.PetId, pet.PetName, vaccine.VaccineId,
                    vaccine.VaccineName, vaccine.Status, vaccine.Eligible,
                    vaccine.RecommendedDueAtUtc, vaccine.NextDueAtUtc,
                    vaccine.DaysRemaining, vaccine.DaysOverdue)));
        }

        return Result.Success<IReadOnlyCollection<VaccinationReminderCandidateResponse>>(candidates);
    }

    private static bool IsActionable(VaccinationStatusVaccineResponse vaccine) =>
        vaccine.Status is nameof(VaccinationStatus.DueSoon)
            or nameof(VaccinationStatus.DueToday)
            or nameof(VaccinationStatus.Overdue) ||
        vaccine.Status == nameof(VaccinationStatus.NotStarted) && vaccine.Eligible;
}
