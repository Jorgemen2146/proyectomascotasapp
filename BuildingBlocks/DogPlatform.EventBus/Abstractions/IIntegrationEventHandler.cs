using DogPlatform.Contracts.IntegrationEvents;

namespace DogPlatform.EventBus.Abstractions;

public interface IIntegrationEventHandler<in T> where T : IntegrationEvent
{
    Task HandleAsync(T integrationEvent, CancellationToken cancellationToken = default);
}
