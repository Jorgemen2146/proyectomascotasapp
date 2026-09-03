namespace DogPlatform.Identity.Infrastructure.Messaging;

internal sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; init; } = "Resend";
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "PetLife";
    public string VerificationCodeHashKey { get; init; } = string.Empty;
    public ResendOptions Resend { get; init; } = new();
}

internal sealed class ResendOptions
{
    public string ApiKey { get; init; } = string.Empty;
}
