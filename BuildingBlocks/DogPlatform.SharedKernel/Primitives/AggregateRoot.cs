namespace DogPlatform.SharedKernel.Primitives;

/// <summary>
/// Base class for aggregate roots.
/// Aggregate roots are the only entry point for modifying the aggregate.
/// They collect domain events raised during the operation.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id) { }

    // Required for ORM hydration
    protected AggregateRoot() { }

    /// <summary>
    /// Domain events raised during the current operation.
    /// These should be dispatched after the aggregate is persisted.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    /// <summary>
    /// Registers a domain event to be dispatched after persistence.
    /// </summary>
    protected void Raise(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <summary>
    /// Clears all collected domain events.
    /// Called by the infrastructure layer after events have been dispatched.
    /// </summary>
    public void ClearDomainEvents() =>
        _domainEvents.Clear();
}
