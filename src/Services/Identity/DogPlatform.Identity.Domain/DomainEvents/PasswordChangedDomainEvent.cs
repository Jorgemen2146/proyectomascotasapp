using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.DomainEvents;

/// <summary>
/// Raised when a user changes their password.
/// Consumers may use this event to revoke all active refresh tokens
/// as a security measure.
/// </summary>
public sealed record PasswordChangedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid UserId) : IDomainEvent;
