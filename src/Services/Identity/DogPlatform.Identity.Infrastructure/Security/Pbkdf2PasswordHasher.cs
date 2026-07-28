using DogPlatform.Identity.Application.Security;
using System.Security.Cryptography;

namespace DogPlatform.Identity.Infrastructure.Security;

internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private const int Iterations = 350_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public PasswordHashResult Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            HashSize);

        return new PasswordHashResult(
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);

        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            Algorithm,
            HashSize);

        var storedHash = Convert.FromBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(hashBytes, storedHash);
    }
}
