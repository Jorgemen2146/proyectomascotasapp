namespace DogPlatform.Identity.Application.Security;

public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password using a cryptographic algorithm.
    /// Returns a tuple of (hash, salt).
    /// </summary>
    (string Hash, string Salt) HashPassword(string password);

    /// <summary>
    /// Verifies that a plaintext password matches the stored hash using the provided salt.
    /// </summary>
    bool VerifyPassword(string password, string hash, string salt);
}
