using System.Text;
using DogPlatform.Identity.Application;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Infrastructure.Authentication;
using DogPlatform.Identity.Infrastructure.Persistence.Context;
using DogPlatform.Identity.Infrastructure.Persistence.Repositories;
using DogPlatform.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

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
        services.AddSingleton<IRefreshTokenGenerator, SecureRefreshTokenGenerator>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var secret = jwtSection["Secret"] ?? string.Empty;
        var issuer = jwtSection["Issuer"] ?? string.Empty;
        var audience = jwtSection["Audience"] ?? string.Empty;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }
}


