using DogPlatform.Health.Application.Features.Vaccinations;
using DogPlatform.Health.Application.Services;
using DogPlatform.Health.Domain.Entities;
using DogPlatform.Health.Domain.Enums;
using DogPlatform.Health.Domain.Repositories;
using Xunit;

namespace DogPlatform.Health.Tests;

public sealed class VaccinationScheduleServiceTests
{
    private static readonly DateTime Applied = new(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc);
    private readonly VaccinationScheduleService _sut = new();

    [Fact]
    public void Uses_interval_from_next_configured_dose()
    {
        var schedules = new[] { Schedule(1), Schedule(2, interval: 28) };
        Assert.Equal(Applied.AddDays(28), _sut.CalculateNextDueDate(Applied, 1, schedules));
    }

    [Fact]
    public void Uses_booster_on_last_initial_dose()
    {
        var schedules = new[] { Schedule(1), Schedule(2, interval: 28, booster: 365) };
        Assert.Equal(Applied.AddDays(365), _sut.CalculateNextDueDate(Applied, 2, schedules));
    }

    [Fact]
    public void Returns_null_without_sufficient_schedule()
    {
        Assert.Null(_sut.CalculateNextDueDate(Applied, 1, Array.Empty<VaccineSchedule>()));
    }

    [Fact]
    public void Changed_applied_date_changes_due_date()
    {
        var schedules = new[] { Schedule(1), Schedule(2, interval: 21) };
        Assert.Equal(Applied.AddDays(31), _sut.CalculateNextDueDate(Applied.AddDays(10), 1, schedules));
    }

    private static VaccineSchedule Schedule(int dose, int? interval = null, int? booster = null) =>
        new(dose, 1, dose, null, interval, booster, true, Applied);
}

public sealed class VaccinationReminderCandidateTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PetId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateOnly Date = new(2026, 8, 23);
    private static readonly DateTime Now = Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    [Fact]
    public async Task Internal_candidates_include_actionable_status_with_owner()
    {
        var records = new ReminderRecords();
        records.Items.Add(PetVaccination.Create(
            PetId, 1, 1, Now.AddDays(-20), Now.AddDays(3),
            null, null, null, null, Now.AddDays(-20)));
        var handler = CreateHandler(records, Now.AddYears(-2));

        var result = await handler.Handle(new(Date), default);

        var candidate = Assert.Single(result.Value);
        Assert.Equal(UserId, candidate.UserId);
        Assert.Equal(nameof(VaccinationStatus.DueSoon), candidate.Status);
        Assert.Equal(3, candidate.DaysRemaining);
    }

    [Fact]
    public async Task Internal_candidates_exclude_not_started_when_pet_is_not_eligible()
    {
        var handler = CreateHandler(new ReminderRecords(), Now.AddDays(-7));
        var result = await handler.Handle(new(Date), default);
        Assert.Empty(result.Value);
    }

    private static GetVaccinationReminderCandidatesQueryHandler CreateHandler(
        ReminderRecords records, DateTime birthDate) =>
        new(new ReminderPetCatalog(birthDate), records, new ReminderVaccines(),
            new VaccinationStatusService());

    private sealed class ReminderPetCatalog(DateTime birthDate) : IInternalPetCatalogService
    {
        public Task<IReadOnlyCollection<InternalPetVaccinationContext>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<InternalPetVaccinationContext>>(
                [new(UserId, PetId, 1, birthDate, "Andrea Kitty")]);
    }

    private sealed class ReminderVaccines : IVaccineRepository
    {
        private readonly Vaccine _vaccine = new(1, 1, "Rabia", null, true, true, Now);
        public Task<IReadOnlyCollection<Vaccine>> GetActiveBySpeciesAsync(
            int speciesId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Vaccine>>([_vaccine]);
        public Task<Vaccine?> GetActiveByIdAsync(
            int vaccineId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Vaccine?>(_vaccine);
        public Task<IReadOnlyCollection<VaccineSchedule>> GetActiveSchedulesAsync(
            int vaccineId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<VaccineSchedule>>(
                [new VaccineSchedule(1, 1, 1, 12, null, null, true, Now)]);
    }

    private sealed class ReminderRecords : IPetVaccinationRepository
    {
        public List<PetVaccination> Items { get; } = [];
        public Task<IReadOnlyCollection<PetVaccination>> GetByPetIdAsync(
            Guid petId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<PetVaccination>>(
                Items.Where(x => x.PetId == petId).ToArray());
        public Task<PetVaccination?> GetByIdAsync(Guid petId, Guid petVaccinationId,
            CancellationToken cancellationToken = default) => Task.FromResult<PetVaccination?>(null);
        public Task<bool> ExactDuplicateExistsAsync(Guid petId, int vaccineId,
            DateTime appliedAtUtc, int? doseNumber, Guid? excludingId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(PetVaccination vaccination,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

public sealed class VaccinationStatusServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc);
    private readonly VaccinationStatusService _sut = new();

    [Fact] public void Up_to_date() => Assert.Equal(VaccinationStatus.UpToDate, _sut.GetVaccinationStatus(Now.AddDays(8), Now, true).Status);
    [Fact] public void Due_soon() => Assert.Equal(VaccinationStatus.DueSoon, _sut.GetVaccinationStatus(Now.AddDays(4), Now, true).Status);
    [Fact] public void Due_today() => Assert.Equal(VaccinationStatus.DueToday, _sut.GetVaccinationStatus(Now.AddHours(8), Now, true).Status);
    [Fact] public void Overdue() => Assert.Equal(VaccinationStatus.Overdue, _sut.GetVaccinationStatus(Now.AddDays(-1), Now, true).Status);
}

public sealed class VaccinationHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid PetId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Create_succeeds_and_calculates_next_due()
    {
        var fixture = new Fixture();
        var result = await fixture.CreateHandler().Handle(Command(), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(Now.AddDays(-1).AddDays(28), result.Value.NextDueAtUtc);
        Assert.Single(fixture.Records.Items);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Cat_cannot_receive_dog_vaccine()
    {
        var fixture = new Fixture();
        fixture.Access.Result = PetAccessResult.Accessible(new PetHealthData(PetId, 2, Now.AddYears(-2), "Michi"));
        var result = await fixture.CreateHandler().Handle(Command(), default);
        Assert.True(result.IsFailure);
        Assert.Equal("VACCINE_SPECIES_MISMATCH", result.Error.Code);
        Assert.Empty(fixture.Records.Items);
    }

    [Fact]
    public async Task Create_fails_for_missing_vaccine()
    {
        var fixture = new Fixture(includeVaccine: false);
        var result = await fixture.CreateHandler().Handle(Command(), default);
        Assert.True(result.IsFailure);
        Assert.Equal("Vaccination.VaccineNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Create_rejects_exact_duplicate()
    {
        var fixture = new Fixture();
        fixture.Records.Items.Add(PetVaccination.Create(PetId, 1, 1, Now.AddDays(-1), null, null, null, null, null, Now));
        var result = await fixture.CreateHandler().Handle(Command(), default);
        Assert.True(result.IsFailure);
        Assert.Equal("Vaccination.Duplicate", result.Error.Code);
    }

    [Fact]
    public async Task Update_recalculates_due_date()
    {
        var fixture = new Fixture();
        var record = PetVaccination.Create(PetId, 1, 1, Now.AddDays(-10), null, null, null, null, null, Now.AddDays(-10));
        fixture.Records.Items.Add(record);
        var applied = Now.AddDays(-2);
        var handler = new UpdateVaccinationCommandHandler(fixture.Records, fixture.Vaccines, fixture.UnitOfWork,
            fixture.Access, fixture.Schedule, fixture.Status, fixture.Time);
        var result = await handler.Handle(new(PetId, record.PetVaccinationId, 1, applied, null, null, null, null), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(applied.AddDays(28), record.NextDueAtUtc);
    }

    [Fact]
    public async Task Delete_is_soft_delete()
    {
        var fixture = new Fixture();
        var record = PetVaccination.Create(PetId, 1, 1, Now.AddDays(-1), null, null, null, null, null, Now);
        fixture.Records.Items.Add(record);
        var handler = new DeleteVaccinationCommandHandler(fixture.Records, fixture.UnitOfWork, fixture.Access, fixture.Time);
        var result = await handler.Handle(new(PetId, record.PetVaccinationId), default);
        Assert.True(result.IsSuccess);
        Assert.True(record.IsDeleted);
        Assert.Equal(Now, record.UpdatedAtUtc);
    }

    [Fact]
    public async Task Older_pet_without_history_is_eligible_not_started_and_counted()
    {
        var fixture = new Fixture();
        fixture.Access.Result = PetAccessResult.Accessible(new PetHealthData(PetId, 1, Now.AddYears(-1), "Firulais"));
        var result = await fixture.StatusHandler().Handle(new(PetId), default);
        Assert.True(result.IsSuccess);
        var vaccine = Assert.Single(result.Value.Vaccines);
        Assert.Equal(nameof(VaccinationStatus.NotStarted), vaccine.Status);
        Assert.True(vaccine.Eligible);
        Assert.Equal(Now.AddYears(-1).Date.AddDays(12 * 7), vaccine.RecommendedDueAtUtc);
        Assert.Equal(0, vaccine.DaysUntilEligible);
        Assert.Null(vaccine.NextDueAtUtc);
        Assert.Equal(1, result.Value.Summary.NotStarted);
    }

    [Fact]
    public async Task Too_young_pet_shows_future_vaccine_without_counting_it_as_not_started()
    {
        var fixture = new Fixture();
        fixture.Access.Result = PetAccessResult.Accessible(new PetHealthData(PetId, 1, Now.AddDays(-7), "Cachorro"));
        var result = await fixture.StatusHandler().Handle(new(PetId), default);
        Assert.True(result.IsSuccess);
        var vaccine = Assert.Single(result.Value.Vaccines);
        Assert.Equal(nameof(VaccinationStatus.NotStarted), vaccine.Status);
        Assert.False(vaccine.Eligible);
        Assert.Equal(Now.AddDays(-7).Date.AddDays(12 * 7), vaccine.RecommendedDueAtUtc);
        Assert.True(vaccine.DaysUntilEligible > 0);
        Assert.Null(vaccine.NextDueAtUtc);
        Assert.Null(vaccine.DaysOverdue);
        Assert.Equal(0, result.Value.Summary.NotStarted);
    }

    [Fact]
    public async Task Pet_at_exact_minimum_age_is_eligible_and_counted_as_not_started()
    {
        var fixture = new Fixture();
        var birthDate = Now.Date.AddDays(-(12 * 7));
        fixture.Access.Result = PetAccessResult.Accessible(new PetHealthData(PetId, 1, birthDate, "Cachorro"));

        var result = await fixture.StatusHandler().Handle(new(PetId), default);

        Assert.True(result.IsSuccess);
        var vaccine = Assert.Single(result.Value.Vaccines);
        Assert.Equal(nameof(VaccinationStatus.NotStarted), vaccine.Status);
        Assert.True(vaccine.Eligible);
        Assert.Equal(Now.Date, vaccine.RecommendedDueAtUtc);
        Assert.Equal(0, vaccine.DaysUntilEligible);
        Assert.Equal(1, result.Value.Summary.NotStarted);
    }

    [Fact]
    public async Task Existing_history_keeps_all_due_statuses()
    {
        var fixture = new Fixture();
        fixture.Vaccines.Items.Clear();
        for (var id = 1; id <= 4; id++)
            fixture.Vaccines.Items.Add(new Vaccine(id, 1, $"Vaccine {id}", null, true, true, Now));
        fixture.Records.Items.Add(PetVaccination.Create(PetId, 1, 1, Now.AddDays(-1), Now.AddDays(8), null, null, null, null, Now));
        fixture.Records.Items.Add(PetVaccination.Create(PetId, 2, 1, Now.AddDays(-1), Now.AddDays(4), null, null, null, null, Now));
        fixture.Records.Items.Add(PetVaccination.Create(PetId, 3, 1, Now.AddDays(-1), Now.AddHours(2), null, null, null, null, Now));
        fixture.Records.Items.Add(PetVaccination.Create(PetId, 4, 1, Now.AddDays(-1), Now.AddDays(-1), null, null, null, null, Now));

        var result = await fixture.StatusHandler().Handle(new(PetId), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Summary.UpToDate);
        Assert.Equal(1, result.Value.Summary.DueSoon);
        Assert.Equal(1, result.Value.Summary.DueToday);
        Assert.Equal(1, result.Value.Summary.Overdue);
        Assert.Equal(0, result.Value.Summary.NotStarted);
        Assert.Equal(
            [nameof(VaccinationStatus.Overdue), nameof(VaccinationStatus.DueToday),
                nameof(VaccinationStatus.DueSoon), nameof(VaccinationStatus.UpToDate)],
            result.Value.Vaccines.Select(x => x.Status));
        Assert.All(result.Value.Vaccines, vaccine =>
        {
            Assert.True(vaccine.Eligible);
            Assert.Null(vaccine.RecommendedDueAtUtc);
            Assert.Null(vaccine.DaysUntilEligible);
        });
    }

    [Theory]
    [InlineData(PetAccessStatus.NotFound, "Vaccination.PetNotFound")]
    [InlineData(PetAccessStatus.Forbidden, "Vaccination.PetForbidden")]
    public async Task Pets_access_failures_are_preserved(PetAccessStatus status, string expectedCode)
    {
        var fixture = new Fixture();
        fixture.Access.Result = new PetAccessResult(status, null);
        var result = await fixture.StatusHandler().Handle(new(PetId), default);
        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    private static CreateVaccinationCommand Command() => new(PetId, 1, 1, Now.AddDays(-1), "Dr. Pérez", "Central", "ABC123", "Sin reacciones");

    private sealed class Fixture
    {
        public Fixture(bool includeVaccine = true)
        {
            if (includeVaccine) Vaccines.Items.Add(new Vaccine(1, 1, "Rabia", null, true, true, Now));
            Vaccines.Schedules = new[]
            {
                new VaccineSchedule(1, 1, 1, 12, null, null, true, Now),
                new VaccineSchedule(2, 1, 2, null, 28, 365, true, Now)
            };
        }
        public FakeRecords Records { get; } = new();
        public FakeVaccines Vaccines { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public FakePetAccess Access { get; } = new();
        public VaccinationScheduleService Schedule { get; } = new();
        public VaccinationStatusService Status { get; } = new();
        public TimeProvider Time { get; } = new FixedTimeProvider(Now);
        public CreateVaccinationCommandHandler CreateHandler() =>
            new(Records, Vaccines, UnitOfWork, Access, Schedule, Status, Time);
        public GetPetVaccinationStatusQueryHandler StatusHandler() =>
            new(Records, Vaccines, Access, Status, Time);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
    private sealed class FakePetAccess : IPetAccessService
    {
        public PetAccessResult Result { get; set; } =
            PetAccessResult.Accessible(new PetHealthData(PetId, 1, Now.AddYears(-2), "Firulais"));
        public Task<PetAccessResult> GetAccessiblePetAsync(Guid petId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result);
    }
    private sealed class FakeUnitOfWork : IHealthUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) { SaveCount++; return Task.CompletedTask; }
    }
    private sealed class FakeVaccines : IVaccineRepository
    {
        public List<Vaccine> Items { get; } = [];
        public IReadOnlyCollection<VaccineSchedule> Schedules { get; set; } = [];
        public Task<IReadOnlyCollection<Vaccine>> GetActiveBySpeciesAsync(int speciesId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Vaccine>>(Items.Where(x => x.SpeciesId == speciesId && x.IsActive).ToArray());
        public Task<Vaccine?> GetActiveByIdAsync(int vaccineId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.VaccineId == vaccineId && x.IsActive));
        public Task<IReadOnlyCollection<VaccineSchedule>> GetActiveSchedulesAsync(int vaccineId, CancellationToken cancellationToken = default) => Task.FromResult(Schedules);
    }
    private sealed class FakeRecords : IPetVaccinationRepository
    {
        public List<PetVaccination> Items { get; } = [];
        public Task<IReadOnlyCollection<PetVaccination>> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<PetVaccination>>(Items.Where(x => x.PetId == petId && !x.IsDeleted).ToArray());
        public Task<PetVaccination?> GetByIdAsync(Guid petId, Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.PetId == petId && x.PetVaccinationId == id && !x.IsDeleted));
        public Task<bool> ExactDuplicateExistsAsync(Guid petId, int vaccineId, DateTime appliedAtUtc, int? doseNumber,
            Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(Items.Any(x => !x.IsDeleted &&
                x.PetId == petId && x.VaccineId == vaccineId && x.AppliedAtUtc == appliedAtUtc && x.DoseNumber == doseNumber && x.PetVaccinationId != excludingId));
        public Task AddAsync(PetVaccination vaccination, CancellationToken cancellationToken = default) { Items.Add(vaccination); return Task.CompletedTask; }
    }
}
