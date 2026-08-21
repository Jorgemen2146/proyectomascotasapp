namespace DogPlatform.Pets.Application.Storage;

public sealed record StoredPhotoResult(string ObjectKey, string ContentType, long ContentLength);
