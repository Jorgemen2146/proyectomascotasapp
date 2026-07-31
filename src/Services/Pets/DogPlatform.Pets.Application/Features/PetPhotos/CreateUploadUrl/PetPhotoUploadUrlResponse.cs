namespace DogPlatform.Pets.Application.Features.PetPhotos.CreateUploadUrl;

public sealed record PetPhotoUploadUrlResponse(
    string ObjectKey,
    string UploadUrl,
    DateTime ExpiresAtUtc,
    IReadOnlyDictionary<string, string> RequiredHeaders);
