namespace DogPlatform.Pets.Application.Storage;

/// <summary>
/// Result returned after generating a pre-signed S3 upload URL.
/// Contains no AWS-specific types.
/// </summary>
public sealed record PresignedUploadResult(
    string ObjectKey,
    string UploadUrl,
    DateTime ExpiresAtUtc,
    IReadOnlyDictionary<string, string> RequiredHeaders);
