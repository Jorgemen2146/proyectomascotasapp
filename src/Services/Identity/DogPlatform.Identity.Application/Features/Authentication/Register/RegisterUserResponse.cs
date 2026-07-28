namespace DogPlatform.Identity.Application.Features.Authentication.Register;

public sealed record RegisterUserResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsEmailConfirmed,
    DateTime CreatedAt);
