using DogPlatform.Identity.Application;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Infrastructure.Persistence.Context;
using DogPlatform.Identity.Infrastructure.Persistence.Repositories;
using DogPlatform.Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("IdentityDb"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "auth")));

        services.AddScoped<IIdentityUnitOfWork>(provider =>
            provider.GetRequiredService<IdentityDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();

        return services;
    }
}
