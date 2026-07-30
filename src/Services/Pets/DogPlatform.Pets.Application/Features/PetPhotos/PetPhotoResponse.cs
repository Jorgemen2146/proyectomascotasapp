namespace DogPlatform.Pets.Application.Features.PetPhotos;

public sealed record PetPhotoResponse(
    Guid PhotoId,
    Guid PetId,
    string Url,
    bool IsMain,
    DateTime CreatedAt);
