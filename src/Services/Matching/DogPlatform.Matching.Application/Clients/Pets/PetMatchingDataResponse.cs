namespace DogPlatform.Matching.Application.Clients.Pets;

/// <summary>
/// Minimal pet data required by Matching, obtained from PetsService.
/// OwnerId is only used internally and never exposed in public responses.
/// </summary>
public sealed record PetMatchingDataResponse(
    Guid PetId,
    Guid OwnerId,
    string Name,
    int BreedId,
    string BreedName,
    string Sex,
    int AgeMonths,
    string? MainPhotoUrl,
    bool IsDeleted,
    bool IsActive);
