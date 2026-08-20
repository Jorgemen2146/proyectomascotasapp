using System.Text;
using System.Text.Json;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.SharedKernel.Primitives;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace DogPlatform.Pets.Infrastructure.Storage;

public sealed class LocalPetPhotoStorage : IPhotoStorageService
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedExtensions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"],
            ["image/webp"] = [".webp"]
        };

    private readonly StorageOptions _options;
    private readonly IDataProtector _uploadProtector;
    private readonly TimeProvider _timeProvider;
    private readonly string _rootPath;

    public LocalPetPhotoStorage(
        IOptions<StorageOptions> options,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _uploadProtector = dataProtectionProvider.CreateProtector(
            "DogPlatform.Pets.LocalPhotoUpload.v1");
        _timeProvider = timeProvider;
        _rootPath = Path.GetFullPath(_options.Local.RootPath);
    }

    public string ProviderName => "Local";

    public Task<Result<PresignedUploadResult>> CreatePresignedUploadAsync(
        Guid userId,
        Guid petId,
        string fileName,
        string contentType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateUpload(fileName, contentType, fileSize, out var extension, out var error))
            return Task.FromResult(Result.Failure<PresignedUploadResult>(error));

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_options.UploadUrlExpirationMinutes);
        var objectKey = $"pets/{userId:D}/{petId:D}/{now:yyyy}/{now:MM}/{Guid.NewGuid():D}{extension}";
        var payload = new UploadTokenPayload(
            userId,
            petId,
            objectKey,
            contentType,
            fileSize,
            expires);
        var token = _uploadProtector.Protect(JsonSerializer.Serialize(payload));
        var uploadUrl = $"{_options.PublicBaseUrl.TrimEnd('/')}/api/v1/pets/{petId:D}/photos/upload/{token}";

        IReadOnlyDictionary<string, string> requiredHeaders =
            new Dictionary<string, string> { ["Content-Type"] = contentType };

        return Task.FromResult(Result.Success(new PresignedUploadResult(
            objectKey,
            uploadUrl,
            "PUT",
            expires,
            requiredHeaders)));
    }

    public Task<bool> ObjectExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            TryResolveObjectPath(objectKey, out var path) && File.Exists(path));
    }

    public Task<bool> DeleteObjectAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryResolveObjectPath(objectKey, out var path))
                return Task.FromResult(false);

            if (File.Exists(path))
                File.Delete(path);

            return Task.FromResult(true);
        }
        catch (IOException)
        {
            return Task.FromResult(false);
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(false);
        }
    }

    public async Task<Result> UploadObjectAsync(
        PhotoUploadRequest request,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        UploadTokenPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<UploadTokenPayload>(
                _uploadProtector.Unprotect(request.UploadToken))
                ?? throw new InvalidOperationException();
        }
        catch
        {
            return Result.Failure(Error.Validation(
                "Storage.InvalidUploadToken", "The upload token is invalid or cannot be read."));
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (payload.ExpiresAtUtc <= now ||
            payload.UserId != request.UserId ||
            payload.PetId != request.PetId ||
            !string.Equals(payload.ContentType, request.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(Error.Validation(
                "Storage.InvalidUploadToken", "The upload token is expired or does not match this request."));
        }

        if (request.ContentLength.HasValue && request.ContentLength.Value != payload.FileSize)
        {
            return Result.Failure(Error.Validation(
                "Storage.FileSizeMismatch", "The uploaded content length does not match the requested file size."));
        }

        if (!TryResolveObjectPath(payload.ObjectKey, out var targetPath))
            return Result.Failure(Error.Validation("Storage.InvalidObjectKey", "The object key is invalid."));

        var directory = Path.GetDirectoryName(targetPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".upload-{Guid.NewGuid():N}.tmp");

        try
        {
            var signature = new byte[12];
            var signatureLength = 0;
            long total = 0;
            var buffer = new byte[81920];

            await using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                buffer.Length,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += read;
                    if (total > _options.MaximumFileSizeBytes || total > payload.FileSize)
                        return Result.Failure(Error.Validation(
                            "Storage.FileTooLarge", "The uploaded file exceeds the permitted size."));

                    if (signatureLength < signature.Length)
                    {
                        var copyLength = Math.Min(read, signature.Length - signatureLength);
                        Buffer.BlockCopy(buffer, 0, signature, signatureLength, copyLength);
                        signatureLength += copyLength;
                    }

                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }

            if (total != payload.FileSize)
                return Result.Failure(Error.Validation(
                    "Storage.FileSizeMismatch", "The uploaded file size does not match the upload request."));

            if (!HasExpectedSignature(payload.ContentType, signature.AsSpan(0, signatureLength)))
                return Result.Failure(Error.Validation(
                    "Storage.ContentSignatureMismatch", "The file content does not match its declared image type."));

            File.Move(temporaryPath, targetPath, false);
            return Result.Success();
        }
        catch (IOException)
        {
            return Result.Failure(Error.Failure(
                "Storage.WriteFailed", "The image could not be written to local storage."));
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Failure(Error.Failure(
                "Storage.AccessDenied", "The application pool cannot write to local photo storage."));
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public Task<Result<PhotoContent>> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveObjectPath(objectKey, out var path) || !File.Exists(path))
            return Task.FromResult(Result.Failure<PhotoContent>(
                Error.NotFound("Storage.ObjectNotFound", "The image was not found.")));

        var extension = Path.GetExtension(path);
        var contentType = AllowedExtensions.First(pair =>
            pair.Value.Contains(extension, StringComparer.OrdinalIgnoreCase)).Key;
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(Result.Success(new PhotoContent(stream, contentType, stream.Length)));
    }

    public string ResolvePublicUrl(string objectKey)
    {
        if (Uri.TryCreate(objectKey, UriKind.Absolute, out _))
            return objectKey;

        if (!TryResolveObjectPath(objectKey, out _))
            return objectKey;

        var encodedKey = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(objectKey));
        var petId = objectKey.Split('/')[2];
        return $"{_options.PublicBaseUrl.TrimEnd('/')}/api/v1/pets/{petId}/photos/content/{encodedKey}";
    }

    private bool TryValidateUpload(
        string fileName,
        string contentType,
        long fileSize,
        out string extension,
        out Error error)
    {
        extension = string.Empty;
        error = Error.None;

        if (!AllowedExtensions.TryGetValue(contentType, out var extensions))
        {
            error = Error.Validation("Storage.InvalidContentType", "Only JPEG, PNG and WebP images are allowed.");
            return false;
        }

        if (fileSize <= 0 || fileSize > _options.MaximumFileSizeBytes)
        {
            error = Error.Validation("Storage.FileTooLarge", "The file must be between 1 byte and 5 MB.");
            return false;
        }

        var requestedExtension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!extensions.Contains(requestedExtension, StringComparer.OrdinalIgnoreCase))
        {
            error = Error.Validation("Storage.ExtensionMismatch", "The file extension does not match its image type.");
            return false;
        }

        extension = requestedExtension == ".jpeg" ? ".jpg" : requestedExtension;
        return true;
    }

    private bool TryResolveObjectPath(string objectKey, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(objectKey) ||
            objectKey.Contains('\\') ||
            objectKey.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(objectKey))
            return false;

        var segments = objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 6 ||
            !string.Equals(segments[0], "pets", StringComparison.Ordinal) ||
            !Guid.TryParseExact(segments[1], "D", out _) ||
            !Guid.TryParseExact(segments[2], "D", out _) ||
            segments[3].Length != 4 || !segments[3].All(char.IsDigit) ||
            segments[4].Length != 2 || !segments[4].All(char.IsDigit))
            return false;

        var extension = Path.GetExtension(segments[5]).ToLowerInvariant();
        if (!AllowedExtensions.Values.SelectMany(value => value).Contains(extension) ||
            !Guid.TryParseExact(Path.GetFileNameWithoutExtension(segments[5]), "D", out _))
            return false;

        var candidate = Path.GetFullPath(Path.Combine(_rootPath, Path.Combine(segments.Skip(2).ToArray())));
        var rootPrefix = _rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        fullPath = candidate;
        return true;
    }

    private static bool HasExpectedSignature(string contentType, ReadOnlySpan<byte> content)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
            "image/png" => content.Length >= 8 && content[..8].SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => content.Length >= 12 &&
                content[..4].SequenceEqual("RIFF"u8) &&
                content.Slice(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    private sealed record UploadTokenPayload(
        Guid UserId,
        Guid PetId,
        string ObjectKey,
        string ContentType,
        long FileSize,
        DateTime ExpiresAtUtc);
}
