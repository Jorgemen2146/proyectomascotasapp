namespace DogPlatform.Pets.API.Requests.PetPhotos;

/// <summary>Request body for confirming a completed S3 upload.</summary>
public sealed record ConfirmUploadRequest(string ObjectKey);
