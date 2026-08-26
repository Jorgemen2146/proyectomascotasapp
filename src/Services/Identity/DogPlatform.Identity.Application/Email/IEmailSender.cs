namespace DogPlatform.Identity.Application.Communication;

public interface IEmailSender
{
    Task SendVerificationCodeAsync(
        string email,
        string code,
        CancellationToken cancellationToken);

    Task SendPasswordResetCodeAsync(
        string email,
        string code,
        int expirationMinutes,
        CancellationToken cancellationToken);
}
