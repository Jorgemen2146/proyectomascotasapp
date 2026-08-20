namespace DogPlatform.Identity.API.Requests.Authentication;

public sealed record UpdateMyProfileRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber);
