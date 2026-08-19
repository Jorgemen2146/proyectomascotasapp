using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Errors;

public static class UserErrors
{
    public static readonly Error NotFound =
        Error.NotFound("User.NotFound", "A user with the specified identifier was not found.");

    public static readonly Error EmailAlreadyExists =
        Error.Conflict("User.EmailAlreadyExists", "A user with the specified email address already exists.");

    public static readonly Error Inactive =
        Error.Unauthorized("User.Inactive", "The user account is inactive and cannot authenticate.");

    public static readonly Error AlreadyInactive =
        Error.Conflict("User.AlreadyInactive", "The user account is already inactive.");

    public static readonly Error EmailAlreadyConfirmed =
        Error.Conflict("User.EmailAlreadyConfirmed", "The email address has already been confirmed.");

    public static readonly Error EmailNotVerified =
        Error.Unauthorized("EMAIL_NOT_VERIFIED", "Debes verificar tu correo antes de iniciar sesión.");

    public static readonly Error EmailVerificationCodeUnavailable =
        Error.Conflict("EmailVerification.CodeUnavailable", "No active verification code is available. Request a new code.");

    public static readonly Error EmailVerificationCodeExpired =
        Error.Conflict("EmailVerification.CodeExpired", "The verification code has expired. Request a new code.");

    public static readonly Error EmailVerificationCodeInvalid =
        Error.Validation("EmailVerification.InvalidCode", "The verification code is invalid.");

    public static readonly Error EmailVerificationAttemptsExceeded =
        Error.Conflict("EmailVerification.AttemptsExceeded", "The verification code is no longer valid. Request a new code.");

    public static readonly Error RoleAlreadyAssigned =
        Error.Conflict("User.RoleAlreadyAssigned", "The specified role is already assigned to this user.");

    public static readonly Error RoleNotAssigned =
        Error.NotFound("User.RoleNotAssigned", "The specified role is not assigned to this user.");
}
