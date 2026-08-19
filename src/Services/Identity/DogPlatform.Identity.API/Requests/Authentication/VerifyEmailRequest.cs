namespace DogPlatform.Identity.API.Requests.Authentication;

public sealed record VerifyEmailRequest(string Email, string Code);
