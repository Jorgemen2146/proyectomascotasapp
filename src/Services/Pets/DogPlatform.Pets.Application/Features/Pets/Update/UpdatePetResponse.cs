namespace DogPlatform.Pets.Application.Features.Pets.Update;

public sealed record UpdatePetResponse(
    Guid PetId,
    string Name);
