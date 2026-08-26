using DogPlatform.Matching.Application.Clients.Genealogy;
using DogPlatform.Matching.Application.Clients.Health;
using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Clients.Identity;
using DogPlatform.Matching.Application.Clients.Notifications;
using DogPlatform.Matching.Application.Options;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.Matching.Infrastructure.Clients;
using DogPlatform.Matching.Infrastructure.Persistence.Context;
using DogPlatform.Matching.Infrastructure.Persistence.Repositories;
using DogPlatform.Matching.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Matching.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Security ─────────────────────────────────────────────────────
        services.AddScoped<ICurrentUser, CurrentUserService>();

        // ── EF Core ──────────────────────────────────────────────────────
        services.AddDbContext<MatchingDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("MatchingDb")));

        services.AddScoped<IMatchingUnitOfWork>(sp => sp.GetRequiredService<MatchingDbContext>());

        // ── Repositories ─────────────────────────────────────────────────
        services.AddScoped<IMatchingProfileRepository, MatchingProfileRepository>();
        services.AddScoped<IFavoriteCandidateRepository, FavoriteCandidateRepository>();
        services.AddScoped<IMatchRequestRepository, MatchRequestRepository>();
        services.AddScoped<IPetMatchRepository, PetMatchRepository>();
        services.AddScoped<IBreedingIntentRepository, BreedingIntentRepository>();

        // ── Outbound typed HTTP clients ─────────────────────────────────
        var timeoutSeconds = configuration.GetValue<int?>("Matching:OutboundTimeoutSeconds") ?? 10;

        var petsBaseUrl = configuration["PetsService:BaseUrl"] ?? "http://localhost:5000";
        services.AddHttpClient<IPetsMatchingClient, PetsMatchingClient>(client =>
        {
            client.BaseAddress = new Uri(petsBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        var genealogyBaseUrl = configuration["GenealogyService:BaseUrl"] ?? "http://localhost:5000";
        services.AddHttpClient<IGenealogyMatchingClient, GenealogyMatchingClient>(client =>
        {
            client.BaseAddress = new Uri(genealogyBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        var internalKey = configuration["InternalServices:ApiKey"];
        static void ConfigureInternalClient(HttpClient client, string baseUrl,
            string? apiKey, int timeout)
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(timeout);
            if (!string.IsNullOrWhiteSpace(apiKey))
                client.DefaultRequestHeaders.Add("X-DogPlatform-Internal-Key", apiKey);
        }

        services.AddHttpClient<IIdentityMatchingClient, IdentityMatchingClient>(client =>
            ConfigureInternalClient(client,
                configuration["IdentityService:BaseUrl"] ?? "http://localhost:5102",
                internalKey, timeoutSeconds));
        services.AddHttpClient<IMatchingNotificationClient, MatchingNotificationClient>(client =>
            ConfigureInternalClient(client,
                configuration["NotificationsService:BaseUrl"] ?? "http://localhost:5109",
                internalKey, timeoutSeconds));

        // Health integration is a neutral stub in v1 (no HTTP calls yet).
        services.AddSingleton<IHealthMatchingClient, HealthMatchingClient>();

        return services;
    }
}
