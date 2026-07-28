namespace DogPlatform.Identity.Application.Security;

public interface IPasswordHasher
{
    PasswordHashResult Hash(string password);

    bool Verify(string password, string hash, string salt);
}
