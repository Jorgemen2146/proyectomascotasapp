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

    public static readonly Error RoleAlreadyAssigned =
        Error.Conflict("User.RoleAlreadyAssigned", "The specified role is already assigned to this user.");

    public static readonly Error RoleNotAssigned =
        Error.NotFound("User.RoleNotAssigned", "The specified role is not assigned to this user.");
}
