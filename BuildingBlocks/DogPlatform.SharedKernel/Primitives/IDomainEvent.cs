using MediatR;

namespace DogPlatform.SharedKernel.Primitives;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
