using System.ComponentModel.DataAnnotations;

namespace DogPlatform.Identity.Application.Features.Authentication.PasswordReset;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    [Range(1, 60)]
    public int CodeExpirationMinutes { get; init; } = 10;

    [Range(1, 20)]
    public int MaxAttempts { get; init; } = 5;

    [Range(1, 3600)]
    public int ResendCooldownSeconds { get; init; } = 60;
}
