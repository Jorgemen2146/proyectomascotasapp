namespace DogPlatform.Pets.Application.Features.Pets.GetById;

public sealed record PetDetailsResponse(
    Guid PetId,
    int BreedId,
    string Name,
    DateTime? BirthDate,
    string Gender,
    decimal? Weight,
    string? Color,
    string? PedigreeNumber,
    bool IsSterilized,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
