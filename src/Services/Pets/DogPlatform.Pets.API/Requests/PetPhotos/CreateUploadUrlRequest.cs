namespace DogPlatform.Pets.API.Requests.PetPhotos;

/// <summary>Request body for generating a pre-signed S3 upload URL.</summary>
public sealed record CreateUploadUrlRequest(
    string FileName,
    string ContentType,
    long FileSize);
