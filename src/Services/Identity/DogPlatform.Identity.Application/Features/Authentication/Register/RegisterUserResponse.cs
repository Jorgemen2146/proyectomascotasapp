namespace DogPlatform.Identity.Application.Features.Authentication.Register;

public sealed record RegisterUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    bool IsEmailConfirmed);
