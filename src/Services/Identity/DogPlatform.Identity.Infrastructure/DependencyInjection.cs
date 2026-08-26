using System.Text;
using DogPlatform.Identity.Application;
using DogPlatform.Identity.Application.Communication;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Application.ProfilePhotos;
using DogPlatform.Identity.Application.Features.Authentication.PasswordReset;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Infrastructure.Messaging;
using DogPlatform.Identity.Infrastructure.Authentication;
using DogPlatform.Identity.Infrastructure.Persistence.Context;
using DogPlatform.Identity.Infrastructure.Persistence.Repositories;
using DogPlatform.Identity.Infrastructure.Security;
using DogPlatform.Identity.Infrastructure.Storage;
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
        services.AddScoped<IPasswordResetCodeRepository, PasswordResetCodeRepository>();
        services.AddScoped<ILegalDocumentRepository, LegalDocumentRepository>();
        services.AddScoped<IUserLegalConsentRepository, UserLegalConsentRepository>();

        services.Configure<ProfileStorageOptions>(
            configuration.GetSection(ProfileStorageOptions.SectionName));
        var profileStorageProvider = configuration[$"{ProfileStorageOptions.SectionName}:Provider"] ?? "Local";
        if (!profileStorageProvider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Unsupported profile storage provider '{profileStorageProvider}'.");
        services.AddScoped<IProfilePhotoStorage, LocalProfilePhotoStorage>();

        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IEmailVerificationCodeService, HmacEmailVerificationCodeService>();
        services.AddSingleton<IPasswordResetCodeService, HmacPasswordResetCodeService>();
        services.AddSingleton<IRefreshTokenGenerator, SecureRefreshTokenGenerator>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.Configure<EmailOptions>(
            configuration.GetSection(EmailOptions.SectionName));
        services.AddOptions<PasswordResetOptions>()
            .Bind(configuration.GetSection(PasswordResetOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ResendEmailSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddScoped<IEmailSender>(provider =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailOptions>>().Value;
            return options.Provider.ToUpperInvariant() switch
            {
                "RESEND" => provider.GetRequiredService<ResendEmailSender>(),
                _ => throw new InvalidOperationException(
                    $"Unsupported email provider '{options.Provider}'.")
            };
        });

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


