namespace DogPlatform.Identity.API.Requests.Authentication;

public sealed record ResetPasswordRequest(
    string Email,
    string Code,
    string NewPassword,
    string ConfirmPassword);
