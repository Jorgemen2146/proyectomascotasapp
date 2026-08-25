using DogPlatform.Health.Application.Services;
using DogPlatform.Health.Domain.Entities;
using DogPlatform.Health.Domain.Enums;
using DogPlatform.Health.Domain.Errors;
using DogPlatform.Health.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Health.Application.Features.Vaccinations;

public sealed class GetVaccinesQueryHandler : IRequestHandler<GetVaccinesQuery, Result<IReadOnlyCollection<VaccineResponse>>>
{
    private readonly IVaccineRepository _vaccines;
    public GetVaccinesQueryHandler(IVaccineRepository vaccines) => _vaccines = vaccines;

    public async Task<Result<IReadOnlyCollection<VaccineResponse>>> Handle(GetVaccinesQuery request, CancellationToken cancellationToken)
    {
        if (request.SpeciesId is not (1 or 2))
            return Result.Failure<IReadOnlyCollection<VaccineResponse>>(VaccinationErrors.InvalidSpeciesId);
        var vaccines = await _vaccines.GetActiveBySpeciesAsync(request.SpeciesId, cancellationToken);
        return Result.Success<IReadOnlyCollection<VaccineResponse>>(vaccines
            .Select(x => new VaccineResponse(x.VaccineId, x.SpeciesId, x.Name, x.Description, x.IsCore)).ToArray());
    }
}

public sealed class GetPetVaccinationsQueryHandler : IRequestHandler<GetPetVaccinationsQuery, Result<IReadOnlyCollection<PetVaccinationResponse>>>
{
    private readonly IPetVaccinationRepository _records;
    private readonly IVaccineRepository _vaccines;
    private readonly IPetAccessService _petAccess;
    private readonly IVaccinationStatusService _status;
    private readonly TimeProvider _time;

    public GetPetVaccinationsQueryHandler(IPetVaccinationRepository records, IVaccineRepository vaccines,
        IPetAccessService petAccess, IVaccinationStatusService status, TimeProvider time)
        => (_records, _vaccines, _petAccess, _status, _time) = (records, vaccines, petAccess, status, time);

    public async Task<Result<IReadOnlyCollection<PetVaccinationResponse>>> Handle(GetPetVaccinationsQuery request, CancellationToken cancellationToken)
    {
        var access = await GetPetHealthData(request.PetId, _petAccess, cancellationToken);
        if (access.IsFailure) return Result.Failure<IReadOnlyCollection<PetVaccinationResponse>>(access.Error);
        var records = await _records.GetByPetIdAsync(request.PetId, cancellationToken);
        var names = new Dictionary<int, string>();
        foreach (var vaccineId in records.Select(x => x.VaccineId).Distinct())
            names[vaccineId] = (await _vaccines.GetActiveByIdAsync(vaccineId, cancellationToken))?.Name ?? "Unknown vaccine";
        var now = _time.GetUtcNow().UtcDateTime;
        return Result.Success<IReadOnlyCollection<PetVaccinationResponse>>(records
            .Select(x => VaccinationMapping.ToResponse(x, names[x.VaccineId], _status, now)).ToArray());
    }

    internal static async Task<Result<PetHealthData>> GetPetHealthData(
        Guid petId,
        IPetAccessService access,
        CancellationToken cancellationToken)
    {
        if (petId == Guid.Empty) return Result.Failure<PetHealthData>(VaccinationErrors.InvalidPetId);
        var result = await access.GetAccessiblePetAsync(petId, cancellationToken);
        return result.Status switch
        {
            PetAccessStatus.Accessible when result.Pet is not null => Result.Success(result.Pet),
            PetAccessStatus.NotFound => Result.Failure<PetHealthData>(VaccinationErrors.PetNotFound),
            PetAccessStatus.Forbidden => Result.Failure<PetHealthData>(VaccinationErrors.PetForbidden),
            PetAccessStatus.Unauthenticated => Result.Failure<PetHealthData>(VaccinationErrors.PetAuthenticationFailed),
            _ => Result.Failure<PetHealthData>(VaccinationErrors.PetsServiceUnavailable)
        };
    }
}

public sealed class CreateVaccinationCommandHandler : IRequestHandler<CreateVaccinationCommand, Result<CreateVaccinationResponse>>
{
    private readonly IPetVaccinationRepository _records;
    private readonly IVaccineRepository _vaccines;
    private readonly IHealthUnitOfWork _unitOfWork;
    private readonly IPetAccessService _petAccess;
    private readonly IVaccinationScheduleService _schedule;
    private readonly IVaccinationStatusService _status;
    private readonly TimeProvider _time;

    public CreateVaccinationCommandHandler(IPetVaccinationRepository records, IVaccineRepository vaccines,
        IHealthUnitOfWork unitOfWork, IPetAccessService petAccess, IVaccinationScheduleService schedule,
        IVaccinationStatusService status, TimeProvider time)
        => (_records, _vaccines, _unitOfWork, _petAccess, _schedule, _status, _time) =
            (records, vaccines, unitOfWork, petAccess, schedule, status, time);

    public async Task<Result<CreateVaccinationResponse>> Handle(CreateVaccinationCommand request, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var validation = VaccinationValidation.Validate(request.PetId, request.DoseNumber, request.AppliedAtUtc, now,
            request.VeterinarianName, request.ClinicName, request.BatchNumber, request.Notes);
        if (validation is not null) return Result.Failure<CreateVaccinationResponse>(validation);
        var access = await GetPetVaccinationsQueryHandler.GetPetHealthData(request.PetId, _petAccess, cancellationToken);
        if (access.IsFailure) return Result.Failure<CreateVaccinationResponse>(access.Error);
        var vaccine = await _vaccines.GetActiveByIdAsync(request.VaccineId, cancellationToken);
        if (vaccine is null) return Result.Failure<CreateVaccinationResponse>(VaccinationErrors.VaccineNotFound);
        if (vaccine.SpeciesId != access.Value.SpeciesId)
            return Result.Failure<CreateVaccinationResponse>(VaccinationErrors.VaccineSpeciesMismatch);
        if (await _records.ExactDuplicateExistsAsync(request.PetId, request.VaccineId, request.AppliedAtUtc,
                request.DoseNumber, null, cancellationToken))
            return Result.Failure<CreateVaccinationResponse>(VaccinationErrors.Duplicate);
        var schedules = await _vaccines.GetActiveSchedulesAsync(request.VaccineId, cancellationToken);
        var nextDue = _schedule.CalculateNextDueDate(request.AppliedAtUtc, request.DoseNumber, schedules);
        var record = PetVaccination.Create(request.PetId, request.VaccineId, request.DoseNumber,
            request.AppliedAtUtc, nextDue, request.VeterinarianName, request.ClinicName,
            request.BatchNumber, request.Notes, now);
        await _records.AddAsync(record, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var status = _status.GetVaccinationStatus(nextDue, now, true);
        return Result.Success(new CreateVaccinationResponse(record.PetVaccinationId, nextDue,
            status.Status.ToString(), status.DaysRemaining, status.DaysOverdue));
    }
}

public sealed class UpdateVaccinationCommandHandler : IRequestHandler<UpdateVaccinationCommand, Result<PetVaccinationResponse>>
{
    private readonly IPetVaccinationRepository _records;
    private readonly IVaccineRepository _vaccines;
    private readonly IHealthUnitOfWork _unitOfWork;
    private readonly IPetAccessService _petAccess;
    private readonly IVaccinationScheduleService _schedule;
    private readonly IVaccinationStatusService _status;
    private readonly TimeProvider _time;

    public UpdateVaccinationCommandHandler(IPetVaccinationRepository records, IVaccineRepository vaccines,
        IHealthUnitOfWork unitOfWork, IPetAccessService petAccess, IVaccinationScheduleService schedule,
        IVaccinationStatusService status, TimeProvider time)
        => (_records, _vaccines, _unitOfWork, _petAccess, _schedule, _status, _time) =
            (records, vaccines, unitOfWork, petAccess, schedule, status, time);

    public async Task<Result<PetVaccinationResponse>> Handle(UpdateVaccinationCommand request, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var validation = VaccinationValidation.Validate(request.PetId, request.DoseNumber, request.AppliedAtUtc, now,
            request.VeterinarianName, request.ClinicName, request.BatchNumber, request.Notes);
        if (validation is not null) return Result.Failure<PetVaccinationResponse>(validation);
        var access = await GetPetVaccinationsQueryHandler.GetPetHealthData(request.PetId, _petAccess, cancellationToken);
        if (access.IsFailure) return Result.Failure<PetVaccinationResponse>(access.Error);
        var record = await _records.GetByIdAsync(request.PetId, request.PetVaccinationId, cancellationToken);
        if (record is null) return Result.Failure<PetVaccinationResponse>(VaccinationErrors.VaccinationNotFound);
        var vaccine = await _vaccines.GetActiveByIdAsync(record.VaccineId, cancellationToken);
        if (vaccine is null) return Result.Failure<PetVaccinationResponse>(VaccinationErrors.VaccineNotFound);
        if (vaccine.SpeciesId != access.Value.SpeciesId)
            return Result.Failure<PetVaccinationResponse>(VaccinationErrors.VaccineSpeciesMismatch);
        if (await _records.ExactDuplicateExistsAsync(request.PetId, record.VaccineId, request.AppliedAtUtc,
                request.DoseNumber, record.PetVaccinationId, cancellationToken))
            return Result.Failure<PetVaccinationResponse>(VaccinationErrors.Duplicate);
        var schedules = await _vaccines.GetActiveSchedulesAsync(record.VaccineId, cancellationToken);
        var nextDue = _schedule.CalculateNextDueDate(request.AppliedAtUtc, request.DoseNumber, schedules);
        record.Update(request.DoseNumber, request.AppliedAtUtc, nextDue, request.VeterinarianName,
            request.ClinicName, request.BatchNumber, request.Notes, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(VaccinationMapping.ToResponse(record, vaccine.Name, _status, now));
    }
}

public sealed class DeleteVaccinationCommandHandler : IRequestHandler<DeleteVaccinationCommand, Result>
{
    private readonly IPetVaccinationRepository _records;
    private readonly IHealthUnitOfWork _unitOfWork;
    private readonly IPetAccessService _petAccess;
    private readonly TimeProvider _time;
    public DeleteVaccinationCommandHandler(IPetVaccinationRepository records, IHealthUnitOfWork unitOfWork,
        IPetAccessService petAccess, TimeProvider time)
        => (_records, _unitOfWork, _petAccess, _time) = (records, unitOfWork, petAccess, time);

    public async Task<Result> Handle(DeleteVaccinationCommand request, CancellationToken cancellationToken)
    {
        var access = await GetPetVaccinationsQueryHandler.GetPetHealthData(request.PetId, _petAccess, cancellationToken);
        if (access.IsFailure) return Result.Failure(access.Error);
        var record = await _records.GetByIdAsync(request.PetId, request.PetVaccinationId, cancellationToken);
        if (record is null) return Result.Failure(VaccinationErrors.VaccinationNotFound);
        record.SoftDelete(_time.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetPetVaccinationStatusQueryHandler : IRequestHandler<GetPetVaccinationStatusQuery, Result<VaccinationStatusResponse>>
{
    private readonly IPetVaccinationRepository _records;
    private readonly IVaccineRepository _vaccines;
    private readonly IPetAccessService _petAccess;
    private readonly IVaccinationStatusService _status;
    private readonly TimeProvider _time;
    public GetPetVaccinationStatusQueryHandler(IPetVaccinationRepository records, IVaccineRepository vaccines,
        IPetAccessService petAccess, IVaccinationStatusService status, TimeProvider time)
        => (_records, _vaccines, _petAccess, _status, _time) = (records, vaccines, petAccess, status, time);

    public async Task<Result<VaccinationStatusResponse>> Handle(GetPetVaccinationStatusQuery request, CancellationToken cancellationToken)
    {
        var access = await GetPetVaccinationsQueryHandler.GetPetHealthData(request.PetId, _petAccess, cancellationToken);
        if (access.IsFailure) return Result.Failure<VaccinationStatusResponse>(access.Error);
        var response = await VaccinationStatusResponseBuilder.BuildAsync(
            access.Value, _records, _vaccines, _status,
            _time.GetUtcNow().UtcDateTime, cancellationToken);
        return Result.Success(response);
    }
}

internal static class VaccinationStatusResponseBuilder
{
    public static async Task<VaccinationStatusResponse> BuildAsync(
        PetHealthData pet,
        IPetVaccinationRepository records,
        IVaccineRepository vaccines,
        IVaccinationStatusService status,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var history = await records.GetByPetIdAsync(pet.PetId, cancellationToken);
        var latest = history.GroupBy(x => x.VaccineId).Select(g => g.OrderByDescending(x => x.AppliedAtUtc).First()).ToList();
        var responses = new List<VaccinationStatusVaccineResponse>();
        foreach (var record in latest)
        {
            var vaccine = await vaccines.GetActiveByIdAsync(record.VaccineId, cancellationToken);
            responses.Add(VaccinationMapping.ToStatusResponse(
                record, vaccine?.Name ?? "Unknown vaccine", status, nowUtc));
        }

        if (pet.BirthDate.HasValue)
        {
            var birthDateUtc = DateTime.SpecifyKind(pet.BirthDate.Value.Date, DateTimeKind.Utc);
            var catalog = await vaccines.GetActiveBySpeciesAsync(pet.SpeciesId, cancellationToken);
            foreach (var vaccine in catalog.Where(v => latest.All(x => x.VaccineId != v.VaccineId)))
            {
                var schedules = await vaccines.GetActiveSchedulesAsync(vaccine.VaccineId, cancellationToken);
                var initial = schedules.Where(x => x.IsActive).OrderBy(x => x.DoseNumber).FirstOrDefault();
                if (initial?.MinAgeWeeks is not int minAgeWeeks)
                    continue;

                var recommendedDueAtUtc = birthDateUtc.AddDays(minAgeWeeks * 7L);
                var eligible = nowUtc >= recommendedDueAtUtc;
                var daysUntilEligible = eligible
                    ? 0
                    : (recommendedDueAtUtc.Date - nowUtc.Date).Days;
                int? daysOverdue = eligible
                    ? Math.Max(0, (nowUtc.Date - recommendedDueAtUtc.Date).Days)
                    : null;
                responses.Add(new(Guid.Empty, pet.PetId, vaccine.VaccineId, vaccine.Name, null,
                    null, null, VaccinationStatus.NotStarted.ToString(), null,
                    daysOverdue, null, null, null, null,
                    eligible, recommendedDueAtUtc, daysUntilEligible));
            }
        }

        var summary = new VaccinationStatusSummary(
            responses.Count(x => x.Status == nameof(VaccinationStatus.UpToDate)),
            responses.Count(x => x.Status == nameof(VaccinationStatus.DueSoon)),
            responses.Count(x => x.Status == nameof(VaccinationStatus.DueToday)),
            responses.Count(x => x.Status == nameof(VaccinationStatus.Overdue)),
            responses.Count(x => x.Status == nameof(VaccinationStatus.NotStarted) && x.Eligible));
        var orderedResponses = responses
            .OrderBy(x => x.Status switch
            {
                nameof(VaccinationStatus.Overdue) => 0,
                nameof(VaccinationStatus.DueToday) => 1,
                nameof(VaccinationStatus.DueSoon) => 2,
                nameof(VaccinationStatus.NotStarted) when x.Eligible => 3,
                nameof(VaccinationStatus.NotStarted) => 4,
                nameof(VaccinationStatus.UpToDate) => 5,
                _ => 6
            })
            .ThenBy(x => x.VaccineName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new VaccinationStatusResponse(pet.PetId, summary, orderedResponses);
    }
}
