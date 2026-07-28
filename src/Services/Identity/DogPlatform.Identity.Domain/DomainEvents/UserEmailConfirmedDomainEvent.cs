using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.DomainEvents;

/// <summary>
/// Raised when a user confirms their email address for the first time.
/// </summary>
public sealed record UserEmailConfirmedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid UserId,
    string Email) : IDomainEvent;
