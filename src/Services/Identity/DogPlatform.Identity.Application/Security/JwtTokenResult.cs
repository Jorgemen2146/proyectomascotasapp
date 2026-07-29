namespace DogPlatform.Identity.Application.Security;

public sealed record JwtTokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc);
