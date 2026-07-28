using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.DomainEvents;

/// <summary>
/// Raised when a user account is deactivated.
/// Consumers may use this event to revoke active refresh tokens,
/// cancel scheduled operations, or send a notification.
/// </summary>
public sealed record UserDeactivatedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid UserId) : IDomainEvent;
