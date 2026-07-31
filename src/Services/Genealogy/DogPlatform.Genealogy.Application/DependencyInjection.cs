using DogPlatform.Genealogy.Application.Features.AssignParents;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Genealogy.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(AssignParentsCommandValidator).Assembly);

        return services;
    }
}
