using DogPlatform.Matching.Application.Evaluation;
using DogPlatform.Matching.Application.Features.CreateMatchRequest;
using DogPlatform.Matching.Application.Options;
using DogPlatform.Matching.Application.Scoring;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DogPlatform.Matching.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(CreateMatchRequestCommandValidator).Assembly);

        services
            .AddOptions<MatchingOptions>()
            .Bind(configuration.GetSection(MatchingOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.MinimumCandidateAgeMonths <= options.MaximumCandidateAgeMonths,
                "MinimumCandidateAgeMonths cannot be greater than MaximumCandidateAgeMonths.")
            .Validate(
                options => options.DefaultMaximumEstimatedInbreedingCoefficient is >= 0 and <= 1,
                "DefaultMaximumEstimatedInbreedingCoefficient must be between 0 and 1.")
            .Validate(
                options => options.DefaultMinimumCompatibilityScore is >= 0 and <= 100,
                "DefaultMinimumCompatibilityScore must be between 0 and 100.")
            .Validate(
                options => options.Weights.Breed >= 0
                    && options.Weights.Age >= 0
                    && options.Weights.Pedigree >= 0
                    && options.Weights.Genealogy >= 0
                    && options.Weights.Health >= 0
                    && options.Weights.Distance >= 0,
                "All matching weights must be non-negative.")
            .Validate(
                options => options.Weights.Sum == 100,
                "The sum of active matching weights (Breed+Age+Pedigree+Genealogy+Health+Distance) must equal 100.")
            .ValidateOnStart();

        services.AddScoped<IMatchScoringService, MatchScoringService>();
        services.AddScoped<CandidateEvaluationService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
