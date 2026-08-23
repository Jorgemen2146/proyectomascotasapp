using DogPlatform.Health.Application.Services;
using DogPlatform.Health.Domain.Repositories;
using DogPlatform.Health.Infrastructure.Persistence;
using DogPlatform.Health.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Health.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HealthDb")
            ?? throw new InvalidOperationException("ConnectionStrings:HealthDb is required.");
        services.AddDbContext<HealthDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IHealthUnitOfWork>(provider => provider.GetRequiredService<HealthDbContext>());
        services.AddScoped<IVaccineRepository, VaccineRepository>();
        services.AddScoped<IPetVaccinationRepository, PetVaccinationRepository>();
        services.AddHttpClient<IPetAccessService, PetsAccessService>(client =>
            client.BaseAddress = new Uri(configuration["PetsService:BaseUrl"] ?? "http://localhost:5103"));
        return services;
    }
}
