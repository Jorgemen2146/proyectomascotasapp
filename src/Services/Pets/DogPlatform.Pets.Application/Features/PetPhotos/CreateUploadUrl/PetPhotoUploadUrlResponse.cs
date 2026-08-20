namespace DogPlatform.Pets.Application.Features.PetPhotos.CreateUploadUrl;

public sealed record PetPhotoUploadUrlResponse(
    string ObjectKey,
    string UploadUrl,
    string Method,
    DateTime ExpiresAtUtc,
    IReadOnlyDictionary<string, string> RequiredHeaders);
