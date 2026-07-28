using DogPlatform.Identity.Application.Features.Authentication.Register;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Register all validators in this assembly
        var validatorTypes = typeof(RegisterUserValidator).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IValidator)))
            .ToList();

        foreach (var validatorType in validatorTypes)
        {
            var interfaceType = validatorType
                .GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

            services.AddScoped(interfaceType, validatorType);
        }

        return services;
    }
}

