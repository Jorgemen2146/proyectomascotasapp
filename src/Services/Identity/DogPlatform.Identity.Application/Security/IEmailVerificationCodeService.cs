namespace DogPlatform.Identity.Application.Security;

public interface IEmailVerificationCodeService
{
    EmailVerificationCodeResult Generate();

    bool Verify(string code, string expectedHash);
}
