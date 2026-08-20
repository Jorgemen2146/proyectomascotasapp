namespace DogPlatform.Identity.Application.Features.Profile.GetMyProfile;

public sealed record MyProfileResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsEmailConfirmed);
