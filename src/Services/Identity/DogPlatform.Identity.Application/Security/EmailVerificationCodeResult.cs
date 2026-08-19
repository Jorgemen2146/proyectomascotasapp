namespace DogPlatform.Identity.Application.Security;

public sealed record EmailVerificationCodeResult(string Code, string Hash);
