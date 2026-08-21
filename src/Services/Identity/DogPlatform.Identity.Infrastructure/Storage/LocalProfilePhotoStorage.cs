using DogPlatform.Identity.Application.ProfilePhotos;
using DogPlatform.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace DogPlatform.Identity.Infrastructure.Storage;

public sealed class LocalProfilePhotoStorage : IProfilePhotoStorage
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

    private readonly string _rootPath;

    public LocalProfilePhotoStorage(IOptions<ProfileStorageOptions> options)
    {
        var configuredPath = options.Value.Local.RootPath;
        if (string.IsNullOrWhiteSpace(configuredPath))
            throw new InvalidOperationException("ProfileStorage:Local:RootPath must be configured.");

        _rootPath = Path.GetFullPath(configuredPath);
    }

    public async Task<Result<StoredProfilePhoto>> SaveAsync(
        Guid userId,
        byte[] content,
        string contentType,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        if (!ProfileImageValidation.TryValidate(
                content, contentType, originalFileName,
                out var extension, out var normalizedContentType, out var error))
            return Result.Failure<StoredProfilePhoto>(error);

        var objectKey = $"profiles/{userId:D}/{Guid.NewGuid():D}{extension}";
        if (!TryResolveObjectPath(objectKey, out var targetPath))
            return Result.Failure<StoredProfilePhoto>(Error.Validation(
                "Profile.Photo.InvalidObjectKey", "The generated object key is invalid."));

        var temporaryPath = targetPath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            await File.WriteAllBytesAsync(temporaryPath, content, cancellationToken);
            File.Move(temporaryPath, targetPath, false);
            return Result.Success(new StoredProfilePhoto(objectKey, normalizedContentType, content.LongLength));
        }
        catch (IOException)
        {
            return Result.Failure<StoredProfilePhoto>(Error.Failure(
                "Profile.Photo.WriteFailed", "The image could not be written to local storage."));
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Failure<StoredProfilePhoto>(Error.Failure(
                "Profile.Photo.AccessDenied", "The application pool cannot write to profile photo storage."));
        }
        finally
        {
            TryDeleteFile(temporaryPath);
        }
    }

    public Task<Result<ProfilePhotoContent>> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveObjectPath(objectKey, out var path) || !File.Exists(path))
            return Task.FromResult(Result.Failure<ProfilePhotoContent>(Error.NotFound(
                "Profile.Photo.NotFound", "The profile photo was not found.")));

        try
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = ContentTypes[Path.GetExtension(path)];
            return Task.FromResult(Result.Success(new ProfilePhotoContent(stream, contentType, stream.Length)));
        }
        catch (IOException)
        {
            return Task.FromResult(Result.Failure<ProfilePhotoContent>(Error.Failure(
                "Profile.Photo.ReadFailed", "The profile photo could not be read.")));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(Result.Failure<ProfilePhotoContent>(Error.Failure(
                "Profile.Photo.AccessDenied", "The application pool cannot read profile photo storage.")));
        }
    }

    public Task<bool> DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (!TryResolveObjectPath(objectKey, out var path))
            return Task.FromResult(false);

        return Task.FromResult(TryDeleteFile(path));
    }

    private bool TryResolveObjectPath(string objectKey, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(objectKey) || objectKey.Contains('\\') ||
            objectKey.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(objectKey))
            return false;

        var segments = objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 3 || segments[0] != "profiles" ||
            !Guid.TryParseExact(segments[1], "D", out _))
            return false;

        var extension = Path.GetExtension(segments[2]).ToLowerInvariant();
        if (!ContentTypes.ContainsKey(extension) ||
            !Guid.TryParseExact(Path.GetFileNameWithoutExtension(segments[2]), "D", out _))
            return false;

        var candidate = Path.GetFullPath(Path.Combine(_rootPath, segments[1], segments[2]));
        var rootPrefix = _rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        fullPath = candidate;
        return true;
    }

    private static bool TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
