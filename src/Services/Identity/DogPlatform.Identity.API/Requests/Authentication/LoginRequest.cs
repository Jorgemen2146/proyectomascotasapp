namespace DogPlatform.Identity.API.Requests.Authentication;

public sealed record LoginRequest(
    string Email,
    string Password);
