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

    public RefreshTokenResult Generate(DateTime utcNow)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return new RefreshTokenResult(token, utcNow.AddDays(_options.RefreshTokenDays));
    }
}
