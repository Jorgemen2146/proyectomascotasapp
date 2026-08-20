namespace DogPlatform.Pets.Application.Storage;

public sealed record PhotoUploadRequest(
    Guid UserId,
    Guid PetId,
    string UploadToken,
    string ContentType,
    long? ContentLength);
