using DogPlatform.Contracts.IntegrationEvents;
using DogPlatform.EventBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace DogPlatform.EventBus.InMemory;

/// <summary>
/// Implementación en memoria del EventBus. Reemplazar por RabbitMQ/Azure Service Bus en producción.
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        _logger.LogInformation(
            "Publishing integration event {EventName} ({EventId}) at {OccurredOn}",
            typeof(T).Name,
            integrationEvent.EventId,
            integrationEvent.OccurredOn);

        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T, THandler>(CancellationToken cancellationToken = default)
        where T : IntegrationEvent
        where THandler : IIntegrationEventHandler<T>
    {
        _logger.LogInformation(
            "Subscribing {HandlerName} to {EventName}",
            typeof(THandler).Name,
            typeof(T).Name);

        return Task.CompletedTask;
    }
}
