namespace DogPlatform.Logging;

public sealed class HttpLoggingOptions
{
    public const string SectionName = "HttpLogging";

    public int MaxRequestBodyBytes { get; set; } = 32 * 1024;
    public bool CaptureRequestBody { get; set; }
    public string ServiceName { get; set; } = string.Empty;
}
