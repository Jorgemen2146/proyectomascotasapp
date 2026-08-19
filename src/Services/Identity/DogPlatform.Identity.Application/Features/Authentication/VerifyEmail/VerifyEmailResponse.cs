namespace DogPlatform.Identity.Application.Features.Authentication.VerifyEmail;

public sealed record VerifyEmailResponse(
    string Email,
    bool IsEmailConfirmed,
    DateTime EmailConfirmedAt);
