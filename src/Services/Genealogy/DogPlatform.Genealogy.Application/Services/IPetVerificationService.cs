namespace DogPlatform.Genealogy.Application.Services;

/// <summary>
/// Checks whether a pet exists in PetsService and, optionally,
/// whether it belongs to the specified owner.
/// Implemented in Infrastructure via HTTP client against PetsService.
/// </summary>
public interface IPetVerificationService
{
    /// <summary>Returns true if the pet exists and is not deleted.</summary>
    Task<bool> PetExistsAsync(Guid petId, CancellationToken cancellationToken = default);

    /// <summary>Returns true if the pet exists and belongs to <paramref name="ownerId"/>.</summary>
    Task<bool> PetBelongsToOwnerAsync(Guid petId, Guid ownerId, CancellationToken cancellationToken = default);
}
