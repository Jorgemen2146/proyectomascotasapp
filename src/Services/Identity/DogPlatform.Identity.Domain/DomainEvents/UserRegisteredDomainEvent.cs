using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.DomainEvents;

/// <summary>
/// Raised when a new user successfully registers in the system.
/// </summary>
public sealed record UserRegisteredDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid UserId,
    string Email,
    string FullName) : IDomainEvent;
