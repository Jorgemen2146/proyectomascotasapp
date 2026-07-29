using System.ComponentModel.DataAnnotations;

namespace DogPlatform.Identity.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    private const int MinSecretLength = 32;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [MinLength(MinSecretLength)]
    public string Secret { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, int.MaxValue)]
    public int RefreshTokenDays { get; init; } = 7;
}
