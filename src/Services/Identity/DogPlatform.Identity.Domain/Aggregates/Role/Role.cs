using DogPlatform.Identity.Domain.Errors;
using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Aggregates.Role;

public sealed class Role : AggregateRoot<Guid>
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 300;

    private Role(Guid id, string name, string? description)
        : base(id)
    {
        Name = name;
        Description = description;
    }

    // Required for ORM hydration.
    private Role() { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static Result<Role> Create(Guid id, string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Role>(RoleErrors.NameEmpty);

        name = name.Trim();

        if (name.Length > NameMaxLength)
            return Result.Failure<Role>(RoleErrors.NameTooLong);

        if (description is not null && description.TrimEnd().Length > DescriptionMaxLength)
            return Result.Failure<Role>(RoleErrors.DescriptionTooLong);

        return Result.Success(new Role(id, name, description?.Trim()));
    }

    // ── Behavior ─────────────────────────────────────────────────────────────

    public Result UpdateDescription(string? description)
    {
        if (description is not null && description.TrimEnd().Length > DescriptionMaxLength)
            return Result.Failure(RoleErrors.DescriptionTooLong);

        Description = description?.Trim();
        return Result.Success();
    }
}
