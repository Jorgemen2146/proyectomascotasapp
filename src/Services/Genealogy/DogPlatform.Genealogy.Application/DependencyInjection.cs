using DogPlatform.Genealogy.Application.Analysis;
using DogPlatform.Genealogy.Application.Features.AssignParents;
using DogPlatform.Genealogy.Application.Features.Relationships;
using DogPlatform.Genealogy.Application.Options;
using DogPlatform.Genealogy.Application.Traversal;
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

        services
            .AddOptions<GenealogyAnalysisOptions>()
            .Bind(configuration.GetSection(GenealogyAnalysisOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.DefaultAnalysisDepth <= options.MaximumAnalysisDepth,
                "DefaultAnalysisDepth cannot be greater than MaximumAnalysisDepth.")
            .Validate(
                options => options.InbreedingWarningThreshold <= options.HighInbreedingThreshold,
                "InbreedingWarningThreshold cannot be greater than HighInbreedingThreshold.")
            .ValidateOnStart();

        services.AddScoped<IGenealogyTraversalService, GenealogyTraversalService>();
        services.AddScoped<IInbreedingCalculator, WrightInbreedingCalculator>();
        services.AddScoped<IKinshipCalculator, WrightKinshipCalculator>();
        services.AddScoped<IPedigreeStatisticsCalculator, PedigreeStatisticsCalculator>();

        services.AddOptions<GenealogyInvitationOptions>()
            .Bind(configuration.GetSection(GenealogyInvitationOptions.SectionName))
            .Validate(options => options.ExpirationHours is >= 1 and <= 720,
                "ExpirationHours must be between 1 and 720.")
            .ValidateOnStart();

        return services;
    }
}
