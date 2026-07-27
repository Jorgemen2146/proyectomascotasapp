using DogPlatform.EventBus.Abstractions;
using DogPlatform.EventBus.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.EventBus;

public static class EventBusExtensions
{
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        return services;
    }
}
