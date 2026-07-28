namespace DogPlatform.SharedKernel.Primitives;

/// <summary>
/// Base class for value objects.
/// Value objects have no identity; equality is based on their component values.
/// They must be immutable.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Returns the atomic values that define equality for this value object.
    /// </summary>
    protected abstract IEnumerable<object?> GetAtomicValues();

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (obj.GetType() != GetType()) return false;

        var other = (ValueObject)obj;
        return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
    }

    public override int GetHashCode()
    {
        return GetAtomicValues()
            .Aggregate(0, (hash, value) =>
                HashCode.Combine(hash, value?.GetHashCode() ?? 0));
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !(left == right);
}
