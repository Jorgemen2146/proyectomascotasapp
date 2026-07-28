using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Aggregates.User;

/// <summary>
/// Represents the assignment of a role to a user.
/// Lives inside the User aggregate boundary.
/// UserRoleId maps to the persistence PK but has no domain significance.
/// </summary>
public sealed class UserRole : Entity<Guid>
{
    internal UserRole(Guid id, Guid userId, Guid roleId, DateTime createdAt)
        : base(id)
    {
        UserId = userId;
        RoleId = roleId;
        CreatedAt = createdAt;
    }

    // Required for ORM hydration.
    private UserRole() { }

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
