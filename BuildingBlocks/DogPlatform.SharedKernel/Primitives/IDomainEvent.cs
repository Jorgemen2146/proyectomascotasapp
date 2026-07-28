namespace DogPlatform.SharedKernel.Primitives;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain
/// and are dispatched after the aggregate is persisted.
/// </summary>
public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOnUtc { get; }
}
