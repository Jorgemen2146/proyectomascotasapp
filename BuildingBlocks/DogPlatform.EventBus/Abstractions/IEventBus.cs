using DogPlatform.Contracts.IntegrationEvents;

namespace DogPlatform.EventBus.Abstractions;

public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IntegrationEvent;

    Task SubscribeAsync<T, THandler>(CancellationToken cancellationToken = default)
        where T : IntegrationEvent
        where THandler : IIntegrationEventHandler<T>;
}
