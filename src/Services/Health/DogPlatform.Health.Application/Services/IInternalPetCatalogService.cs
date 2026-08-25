namespace DogPlatform.Health.Application.Services;

public sealed record InternalPetVaccinationContext(
    Guid UserId,
    Guid PetId,
    int SpeciesId,
    DateTime? BirthDate,
    string PetName);

public interface IInternalPetCatalogService
{
    Task<IReadOnlyCollection<InternalPetVaccinationContext>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
