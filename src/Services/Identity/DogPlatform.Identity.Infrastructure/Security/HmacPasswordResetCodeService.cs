using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace DogPlatform.Identity.Infrastructure.Security;

internal sealed class HmacPasswordResetCodeService : IPasswordResetCodeService
{
    private const int MinimumHashKeyLength = 32;
    private const string DomainPrefix = "password-reset:";
    private readonly byte[] _hashKey;

    public HmacPasswordResetCodeService(IOptions<EmailOptions> options)
    {
        var hashKey = options.Value.VerificationCodeHashKey;
        if (string.IsNullOrWhiteSpace(hashKey) || hashKey.Length < MinimumHashKeyLength)
            throw new InvalidOperationException(
                $"Email:VerificationCodeHashKey must contain at least {MinimumHashKeyLength} characters.");

        _hashKey = Encoding.UTF8.GetBytes(hashKey);
    }

    public PasswordResetCodeResult Generate()
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6", CultureInfo.InvariantCulture);
        return new PasswordResetCodeResult(code, Hash(code));
    }

    public bool Verify(string code, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(expectedHash))
            return false;

        byte[] expectedBytes;
        try { expectedBytes = Convert.FromBase64String(expectedHash); }
        catch (FormatException) { return false; }

        var actualBytes = ComputeHash(code);
        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }

    private string Hash(string code) => Convert.ToBase64String(ComputeHash(code));

    private byte[] ComputeHash(string code)
    {
        using var hmac = new HMACSHA256(_hashKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(DomainPrefix + code));
    }
}
