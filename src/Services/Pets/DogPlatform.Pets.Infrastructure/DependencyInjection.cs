using DogPlatform.Pets.Application;
using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.Pets.Infrastructure.Persistence.Context;
using DogPlatform.Pets.Infrastructure.Persistence.Repositories;
using DogPlatform.Pets.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Pets.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICurrentUser, CurrentUserService>();

        services.AddDbContext<PetsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("PetsDb")));

        services.AddScoped<IPetsUnitOfWork>(sp => sp.GetRequiredService<PetsDbContext>());
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IBreedRepository, BreedRepository>();

        return services;
    }
}
