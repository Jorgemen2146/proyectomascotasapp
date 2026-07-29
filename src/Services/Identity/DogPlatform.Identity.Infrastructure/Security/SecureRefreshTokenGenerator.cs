using System.Security.Cryptography;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace DogPlatform.Identity.Infrastructure.Security;

internal sealed class SecureRefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly JwtOptions _options;

    public SecureRefreshTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public int RefreshTokenDays => _options.RefreshTokenDays;

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
