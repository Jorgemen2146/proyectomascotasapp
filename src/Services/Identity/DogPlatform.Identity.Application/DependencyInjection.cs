using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            ServiceLifetime.Scoped);

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
