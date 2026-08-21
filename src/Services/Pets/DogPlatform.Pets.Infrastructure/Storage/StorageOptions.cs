namespace DogPlatform.Pets.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; init; } = "Local";
    public string PublicBaseUrl { get; init; } = "http://localhost:5101";
    public int UploadUrlExpirationMinutes { get; init; } = 10;
    public long MaximumFileSizeBytes { get; init; } = 10 * 1024 * 1024;
    public LocalStorageOptions Local { get; init; } = new();
}

public sealed class LocalStorageOptions
{
    public string RootPath { get; init; } = string.Empty;
}
