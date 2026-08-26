namespace DogPlatform.Identity.API.Requests.Authentication;

public sealed record VerifyResetCodeRequest(string Email, string Code);
