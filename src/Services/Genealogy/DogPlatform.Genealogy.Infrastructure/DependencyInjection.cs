using DogPlatform.Genealogy.Application;
using DogPlatform.Genealogy.Application.Security;
using DogPlatform.Genealogy.Application.Services;
using DogPlatform.Genealogy.Application.Features.Relationships;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.Genealogy.Infrastructure.Persistence.Context;
using DogPlatform.Genealogy.Infrastructure.Persistence.Repositories;
using DogPlatform.Genealogy.Infrastructure.Security;
using DogPlatform.Genealogy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Genealogy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Security ───────────────────────────────────────────────────────
        services.AddScoped<ICurrentUser, CurrentUserService>();

        // ── EF Core ────────────────────────────────────────────────────────
        services.AddDbContext<GenealogyDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("GenealogyDb")));

        services.AddScoped<IGenealogyUnitOfWork>(sp =>
            sp.GetRequiredService<GenealogyDbContext>());

        // ── Repositories ───────────────────────────────────────────────────
        services.AddScoped<IPetLineageRepository, PetLineageRepository>();
        services.AddScoped<IPetRelationshipRepository, PetRelationshipRepository>();
        services.AddScoped<IRelationshipInvitationRepository, RelationshipInvitationRepository>();
        services.AddSingleton<IInvitationTokenService, InvitationTokenService>();
        services.AddScoped<IGenealogyInvitationEmailSender,
            DevelopmentGenealogyInvitationEmailSender>();
        services.AddScoped<IGenealogyNotificationPublisher,
            DevelopmentGenealogyNotificationPublisher>();

        // ── Pet verification HTTP client ───────────────────────────────────
        var petsBaseUrl = configuration["PetsService:BaseUrl"]
                          ?? "http://localhost:5000";

        services.AddHttpClient<IPetVerificationService, PetVerificationService>(client =>
        {
            client.BaseAddress = new Uri(petsBaseUrl);
        });

        var internalKey = configuration["InternalServices:ApiKey"];
        services.AddHttpClient<IGenealogyPetService, GenealogyPetService>(client =>
        {
            client.BaseAddress = new Uri(petsBaseUrl);
            if (!string.IsNullOrWhiteSpace(internalKey))
                client.DefaultRequestHeaders.Add("X-DogPlatform-Internal-Key", internalKey);
        });

        return services;
    }
}
