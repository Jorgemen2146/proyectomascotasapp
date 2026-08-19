namespace DogPlatform.Identity.Application.Features.Authentication;

public static class EmailVerificationPolicy
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
}
