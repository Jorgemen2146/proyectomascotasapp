using DogPlatform.Health.Domain.Entities;
using DogPlatform.Health.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Health.Infrastructure.Persistence;

public sealed class VaccineRepository : IVaccineRepository
{
    private readonly HealthDbContext _context;
    public VaccineRepository(HealthDbContext context) => _context = context;
    public async Task<IReadOnlyCollection<Vaccine>> GetActiveBySpeciesAsync(int speciesId, CancellationToken cancellationToken = default) =>
        await _context.Vaccines.AsNoTracking().Where(x => x.SpeciesId == speciesId && x.IsActive)
            .OrderByDescending(x => x.IsCore).ThenBy(x => x.Name).ToArrayAsync(cancellationToken);
    public Task<Vaccine?> GetActiveByIdAsync(int vaccineId, CancellationToken cancellationToken = default) =>
        _context.Vaccines.AsNoTracking().FirstOrDefaultAsync(x => x.VaccineId == vaccineId && x.IsActive, cancellationToken);
    public async Task<IReadOnlyCollection<VaccineSchedule>> GetActiveSchedulesAsync(int vaccineId, CancellationToken cancellationToken = default) =>
        await _context.VaccineSchedules.AsNoTracking().Where(x => x.VaccineId == vaccineId && x.IsActive)
            .OrderBy(x => x.DoseNumber).ToArrayAsync(cancellationToken);
}

public sealed class PetVaccinationRepository : IPetVaccinationRepository
{
    private readonly HealthDbContext _context;
    public PetVaccinationRepository(HealthDbContext context) => _context = context;
    public async Task<IReadOnlyCollection<PetVaccination>> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken = default) =>
        await _context.PetVaccinations.AsNoTracking().Where(x => x.PetId == petId)
            .OrderByDescending(x => x.AppliedAtUtc).ToArrayAsync(cancellationToken);
    public Task<PetVaccination?> GetByIdAsync(Guid petId, Guid petVaccinationId, CancellationToken cancellationToken = default) =>
        _context.PetVaccinations.FirstOrDefaultAsync(x => x.PetId == petId && x.PetVaccinationId == petVaccinationId, cancellationToken);
    public Task<bool> ExactDuplicateExistsAsync(Guid petId, int vaccineId, DateTime appliedAtUtc,
        int? doseNumber, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        _context.PetVaccinations.AnyAsync(x => x.PetId == petId && x.VaccineId == vaccineId &&
            x.AppliedAtUtc == appliedAtUtc && x.DoseNumber == doseNumber &&
            (!excludingId.HasValue || x.PetVaccinationId != excludingId.Value), cancellationToken);
    public Task AddAsync(PetVaccination vaccination, CancellationToken cancellationToken = default) =>
        _context.PetVaccinations.AddAsync(vaccination, cancellationToken).AsTask();
}
