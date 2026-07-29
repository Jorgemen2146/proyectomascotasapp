namespace DogPlatform.Pets.Application.Features.Pets.Create;

public sealed record CreatePetResponse(
    Guid PetId,
    string Name);
