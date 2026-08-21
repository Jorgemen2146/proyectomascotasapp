using System.ComponentModel.DataAnnotations;

namespace DogPlatform.Pets.Infrastructure.Storage;

/// <summary>
/// Configuration model for AWS S3 photo storage.
/// Bind from "S3Storage" configuration section.
/// AWS credentials are never stored here — they come from the standard AWS credential chain.
/// </summary>
public sealed class S3StorageOptions
{
    public const string SectionName = "S3Storage";

    /// <summary>When false, S3 endpoints return a controlled error and the application starts without AWS.</summary>
    public bool Enabled { get; init; } = false;

    [Required(ErrorMessage = "S3Storage:BucketName is required when S3Storage:Enabled is true.")]
    public string BucketName { get; init; } = string.Empty;

    [Required(ErrorMessage = "S3Storage:Region is required when S3Storage:Enabled is true.")]
    public string Region { get; init; } = "us-east-1";

    /// <summary>Optional public base URL (e.g. CloudFront). Leave empty to store the object key as the canonical value.</summary>
    public string PublicBaseUrl { get; init; } = string.Empty;

    public int PresignedUrlExpirationMinutes { get; init; } = 10;

    public long MaximumFileSizeBytes { get; init; } = 10 * 1024 * 1024;

    public IReadOnlyList<string> AllowedContentTypes { get; init; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];
}
