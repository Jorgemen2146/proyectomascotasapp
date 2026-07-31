using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Pets.Application.Storage;

/// <summary>
/// Infrastructure-independent abstraction for photo object storage.
/// Implementations must not expose AWS SDK types.
/// </summary>
public interface IPhotoStorageService
{
    /// <summary>
    /// Generates a pre-signed HTTP PUT URL for uploading a photo directly to storage.
    /// The object key is derived from userId + petId and is safe — it never trusts client input.
    /// </summary>
    Task<Result<PresignedUploadResult>> CreatePresignedUploadAsync(
        Guid userId,
        Guid petId,
        string fileName,
        string contentType,
        long fileSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the specified object key exists in storage.
    /// </summary>
    Task<bool> ObjectExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to delete the specified object key from storage.
    /// Does not throw on failure — callers must handle a false return value.
    /// </summary>
    Task<bool> DeleteObjectAsync(
        string objectKey,
        CancellationToken cancellationToken = default);
}
