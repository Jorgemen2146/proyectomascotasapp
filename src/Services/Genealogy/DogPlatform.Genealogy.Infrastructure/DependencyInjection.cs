using DogPlatform.Genealogy.Application;
using DogPlatform.Genealogy.Application.Security;
using DogPlatform.Genealogy.Application.Services;
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

        // ── Pet verification HTTP client ───────────────────────────────────
        var petsBaseUrl = configuration["PetsService:BaseUrl"]
                          ?? "http://localhost:5000";

        services.AddHttpClient<IPetVerificationService, PetVerificationService>(client =>
        {
            client.BaseAddress = new Uri(petsBaseUrl);
        });

        return services;
    }
}
