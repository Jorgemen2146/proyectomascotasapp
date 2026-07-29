namespace DogPlatform.Pets.API.Requests.Pets;

public sealed record CreatePetRequest(
    int BreedId,
    string Name,
    DateTime? BirthDate,
    string Gender,
    decimal? Weight,
    string? Color,
    string? PedigreeNumber,
    bool IsSterilized,
    string? Description);
