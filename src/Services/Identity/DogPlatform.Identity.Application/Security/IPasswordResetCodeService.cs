namespace DogPlatform.Identity.Application.Security;

public sealed record PasswordResetCodeResult(string Code, string Hash);

public interface IPasswordResetCodeService
{
    PasswordResetCodeResult Generate();
    bool Verify(string code, string expectedHash);
}
