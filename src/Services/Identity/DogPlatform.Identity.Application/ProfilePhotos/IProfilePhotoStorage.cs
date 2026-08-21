using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Application.ProfilePhotos;

public interface IProfilePhotoStorage
{
    Task<Result<StoredProfilePhoto>> SaveAsync(
        Guid userId,
        byte[] content,
        string contentType,
        string originalFileName,
        CancellationToken cancellationToken = default);

    Task<Result<ProfilePhotoContent>> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default);
}

public sealed record StoredProfilePhoto(string ObjectKey, string ContentType, long ContentLength);

public sealed record ProfilePhotoContent(Stream Stream, string ContentType, long ContentLength);
