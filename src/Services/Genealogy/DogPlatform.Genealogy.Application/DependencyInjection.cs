using DogPlatform.Genealogy.Application.Features.AssignParents;
using DogPlatform.Genealogy.Application.Options;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DogPlatform.Genealogy.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(AssignParentsCommandValidator).Assembly);

        services
            .AddOptions<GenealogyOptions>()
            .Bind(configuration.GetSection(GenealogyOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.DefaultTreeDepth <= options.MaximumTreeDepth,
                "DefaultTreeDepth cannot be greater than MaximumTreeDepth.")
            .ValidateOnStart();

        return services;
    }
}
