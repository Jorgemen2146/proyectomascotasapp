namespace DogPlatform.Identity.Infrastructure.Storage;

public sealed class ProfileStorageOptions
{
    public const string SectionName = "ProfileStorage";
    public string Provider { get; init; } = "Local";
    public LocalProfileStorageOptions Local { get; init; } = new();
}

public sealed class LocalProfileStorageOptions
{
    public string RootPath { get; init; } = string.Empty;
}
