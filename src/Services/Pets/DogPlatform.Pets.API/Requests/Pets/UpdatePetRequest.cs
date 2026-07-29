namespace DogPlatform.Pets.API.Requests.Pets;

public sealed record UpdatePetRequest(
    string Name,
    DateTime? BirthDate,
    string Gender,
    decimal? Weight,
    string? Color,
    string? PedigreeNumber,
    bool IsSterilized,
    string? Description);
