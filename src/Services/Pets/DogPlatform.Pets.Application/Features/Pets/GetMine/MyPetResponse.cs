namespace DogPlatform.Pets.Application.Features.Pets.GetMine;

public sealed record MyPetResponse(
    Guid PetId,
    int BreedId,
    string Name,
    DateTime? BirthDate,
    string Gender);
