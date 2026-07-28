using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Errors;

public static class RoleErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Role.NotFound", "A role with the specified identifier was not found.");

    public static readonly Error NameAlreadyExists =
        Error.Conflict("Role.NameAlreadyExists", "A role with the specified name already exists.");

    public static readonly Error NameEmpty =
        Error.Validation("Role.NameEmpty", "Role name cannot be empty.");

    public static readonly Error NameTooLong =
        Error.Validation("Role.NameTooLong", "Role name cannot exceed 100 characters.");

    public static readonly Error DescriptionTooLong =
        Error.Validation("Role.DescriptionTooLong", "Role description cannot exceed 300 characters.");
}
