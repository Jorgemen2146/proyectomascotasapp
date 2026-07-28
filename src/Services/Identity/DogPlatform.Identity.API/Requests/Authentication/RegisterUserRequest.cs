namespace DogPlatform.Identity.API.Requests.Authentication;

public sealed record RegisterUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber = null);
