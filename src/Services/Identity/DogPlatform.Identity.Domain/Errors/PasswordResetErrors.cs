using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Errors;

public static class PasswordResetErrors
{
    public static readonly Error InvalidCode =
        Error.Validation("PASSWORD_RESET_CODE_INVALID", "The password reset code is invalid.");

    public static readonly Error ExpiredCode =
        Error.Validation("PASSWORD_RESET_CODE_EXPIRED", "The password reset code has expired.");

    public static readonly Error LockedCode =
        Error.Validation("PASSWORD_RESET_CODE_LOCKED", "The password reset code is locked.");

    public static readonly Error InvalidPassword =
        Error.Validation("PASSWORD_RESET_PASSWORD_INVALID", "The new password does not satisfy the password policy.");
}
