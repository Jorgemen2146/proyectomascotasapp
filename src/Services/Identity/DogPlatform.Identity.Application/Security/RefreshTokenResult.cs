namespace DogPlatform.Identity.Application.Security;

public sealed record RefreshTokenResult(
    string Token,
    DateTime ExpiresAtUtc);
