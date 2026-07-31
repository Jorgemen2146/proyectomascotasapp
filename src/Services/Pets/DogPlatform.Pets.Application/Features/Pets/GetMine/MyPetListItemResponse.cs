namespace DogPlatform.Pets.Application.Features.Pets.GetMine;

public sealed record MyPetListItemResponse(
    Guid Id,
    string Name,
    int SpeciesId,
    string SpeciesName,
    int BreedId,
    string BreedName,
    string Sex,
    DateTime? BirthDate,
    string? MainPhotoUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
