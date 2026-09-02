namespace DogPlatform.Identity.Infrastructure.Authentication.External;

public sealed class GoogleExternalAuthOptions
{
    public const string SectionName = "ExternalAuth:Google";
    public string[] ClientIds { get; init; } = [];
}

public sealed class AppleExternalAuthOptions
{
    public const string SectionName = "ExternalAuth:Apple";
    public string[] ClientIds { get; init; } = [];
}

public sealed class FacebookExternalAuthOptions
{
    public const string SectionName = "ExternalAuth:Facebook";
    public string AppId { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;
    public string GraphApiVersion { get; init; } = "v23.0";
}

public sealed class ExternalRegistrationOptions
{
    public const string SectionName = "ExternalAuth:Registration";
    public string TicketSecret { get; init; } = string.Empty;
    public int TicketLifetimeMinutes { get; init; } = 10;
}
