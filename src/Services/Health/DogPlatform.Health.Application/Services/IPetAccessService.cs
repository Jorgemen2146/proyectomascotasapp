namespace DogPlatform.Health.Application.Services;

public enum PetAccessStatus
{
    Accessible,
    NotFound,
    Forbidden,
    Unauthenticated,
    Unavailable
}

public sealed record PetHealthData(Guid PetId, int SpeciesId, DateTime? BirthDate, string Name);

public sealed record PetAccessResult(PetAccessStatus Status, PetHealthData? Pet)
{
    public static PetAccessResult Accessible(PetHealthData pet) => new(PetAccessStatus.Accessible, pet);
    public static PetAccessResult NotFound() => new(PetAccessStatus.NotFound, null);
    public static PetAccessResult Forbidden() => new(PetAccessStatus.Forbidden, null);
    public static PetAccessResult Unauthenticated() => new(PetAccessStatus.Unauthenticated, null);
    public static PetAccessResult Unavailable() => new(PetAccessStatus.Unavailable, null);
}

public interface IPetAccessService
{
    Task<PetAccessResult> GetAccessiblePetAsync(Guid petId, CancellationToken cancellationToken = default);
}
