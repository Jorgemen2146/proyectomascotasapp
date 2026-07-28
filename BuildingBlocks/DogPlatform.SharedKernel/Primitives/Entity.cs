namespace DogPlatform.SharedKernel.Primitives;

/// <summary>
/// Base class for all domain entities.
/// Equality is based on identity (Id), not reference.
/// </summary>
public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id)
    {
        Id = id;
    }

    // Required for ORM hydration
    protected Entity() { }

    public TId Id { get; private set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;

        var other = (Entity<TId>)obj;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !(left == right);
}
