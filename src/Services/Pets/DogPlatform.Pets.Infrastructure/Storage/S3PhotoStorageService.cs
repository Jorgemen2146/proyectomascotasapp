using Amazon.S3;
using Amazon.S3.Model;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DogPlatform.Pets.Infrastructure.Storage;

/// <summary>
/// AWS S3 implementation of <see cref="IPhotoStorageService"/>.
/// Uses the standard AWS credential chain — no credentials are stored in this class.
/// </summary>
public sealed class S3PhotoStorageService : IPhotoStorageService
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private static readonly Dictionary<string, string> ContentTypeExtensions = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly IAmazonS3 _s3;
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3PhotoStorageService> _logger;

    public string ProviderName => "S3";

    public S3PhotoStorageService(
        IAmazonS3 s3,
        IOptions<S3StorageOptions> options,
        ILogger<S3PhotoStorageService> logger)
    {
        _s3 = s3;
        _options = options.Value;
        _logger = logger;
    }

    public Task<Result<PresignedUploadResult>> CreatePresignedUploadAsync(
        Guid userId,
        Guid petId,
        string fileName,
        string contentType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return Task.FromResult(Result.Failure<PresignedUploadResult>(
                Error.Failure("Storage.Disabled", "Photo storage is not enabled in this environment.")));

        // Validate content type against configuration
        if (!AllowedContentTypes.Contains(contentType) ||
            !_options.AllowedContentTypes.Contains(contentType))
        {
            return Task.FromResult(Result.Failure<PresignedUploadResult>(
                Error.Validation("Storage.InvalidContentType",
                    $"Content type '{contentType}' is not allowed.")));
        }

        if (fileSize <= 0)
        {
            return Task.FromResult(Result.Failure<PresignedUploadResult>(
                Error.Validation("Storage.InvalidFileSize", "File size must be greater than zero.")));
        }

        if (fileSize > _options.MaximumFileSizeBytes)
        {
            return Task.FromResult(Result.Failure<PresignedUploadResult>(
                Error.Validation("Storage.FileTooLarge",
                    $"File size exceeds the maximum allowed size of {_options.MaximumFileSizeBytes / 1024 / 1024} MB.")));
        }

        // Validate extension matches declared content type
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!ContentTypeExtensions.TryGetValue(contentType, out var expectedExtension) ||
            extension != expectedExtension)
        {
            return Task.FromResult(Result.Failure<PresignedUploadResult>(
                Error.Validation("Storage.ExtensionMismatch",
                    $"File extension '{extension}' does not match content type '{contentType}'.")));
        }

        // Build a safe, server-controlled object key — never trust client file names as the key
        var now = DateTime.UtcNow;
        var safeKey = $"pets/{userId}/{petId}/{now:yyyy}/{now:MM}/{Guid.NewGuid()}{expectedExtension}";

        var expires = now.AddMinutes(_options.PresignedUrlExpirationMinutes);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = safeKey,
            Verb = HttpVerb.PUT,
            Expires = expires,
            ContentType = contentType
        };

        string uploadUrl;
        try
        {
            uploadUrl = _s3.GetPreSignedURL(request);
        }
        catch (Exception ex)
        {
            // Log without exposing the URL or credentials
            _logger.LogError(ex,
                "Failed to generate pre-signed upload URL for pet {PetId}, user {UserId}.",
                petId, userId);
            return Task.FromResult(Result.Failure<PresignedUploadResult>(
                Error.Failure("Storage.PresignFailed", "Could not generate an upload URL. Please try again.")));
        }

        _logger.LogInformation(
            "Pre-signed upload URL generated for pet {PetId}, user {UserId}, objectKey {ObjectKey}, expires {ExpiresAt}.",
            petId, userId, safeKey, expires);

        var requiredHeaders = new Dictionary<string, string>
        {
            ["Content-Type"] = contentType
        };

        var result = new PresignedUploadResult(
            safeKey,
            uploadUrl,
            "PUT",
            expires,
            requiredHeaders.AsReadOnly());

        return Task.FromResult(Result.Success(result));
    }

    public async Task<bool> ObjectExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return false;

        try
        {
            var metaRequest = new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey
            };

            await _s3.GetObjectMetadataAsync(metaRequest, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking existence of object {ObjectKey} in S3.", objectKey);
            return false;
        }
    }

    public async Task<bool> DeleteObjectAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return true; // Nothing to delete if storage is disabled

        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey
            };

            await _s3.DeleteObjectAsync(deleteRequest, cancellationToken);

            _logger.LogInformation("Deleted S3 object {ObjectKey}.", objectKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete S3 object {ObjectKey}.", objectKey);
            return false;
        }
    }

    public Task<Result> UploadObjectAsync(
        PhotoUploadRequest request,
        Stream content,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure(
            Error.Validation("Storage.DirectUploadUnsupported", "Direct API uploads are not used by the S3 provider.")));

    public Task<Result<PhotoContent>> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<PhotoContent>(
            Error.Validation("Storage.ContentProxyUnsupported", "Content proxying is not used by the S3 provider.")));

    public string ResolvePublicUrl(string objectKey)
    {
        if (Uri.TryCreate(objectKey, UriKind.Absolute, out _))
            return objectKey;

        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return objectKey;

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{objectKey.TrimStart('/')}";
    }
}
