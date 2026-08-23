using DogPlatform.Health.Domain.Entities;

namespace DogPlatform.Health.Domain.Repositories;

public interface IVaccineRepository
{
    Task<IReadOnlyCollection<Vaccine>> GetActiveBySpeciesAsync(int speciesId, CancellationToken cancellationToken = default);
    Task<Vaccine?> GetActiveByIdAsync(int vaccineId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<VaccineSchedule>> GetActiveSchedulesAsync(int vaccineId, CancellationToken cancellationToken = default);
}

public interface IPetVaccinationRepository
{
    Task<IReadOnlyCollection<PetVaccination>> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken = default);
    Task<PetVaccination?> GetByIdAsync(Guid petId, Guid petVaccinationId, CancellationToken cancellationToken = default);
    Task<bool> ExactDuplicateExistsAsync(Guid petId, int vaccineId, DateTime appliedAtUtc,
        int? doseNumber, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(PetVaccination vaccination, CancellationToken cancellationToken = default);
}

public interface IHealthUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
