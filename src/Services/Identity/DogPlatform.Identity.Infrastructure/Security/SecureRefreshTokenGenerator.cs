using System.Security.Cryptography;
using DogPlatform.Identity.Application.Security;

namespace DogPlatform.Identity.Infrastructure.Security;

internal sealed class SecureRefreshTokenGenerator : IRefreshTokenGenerator
{
    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
