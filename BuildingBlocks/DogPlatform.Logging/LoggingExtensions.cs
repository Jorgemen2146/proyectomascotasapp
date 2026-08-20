using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace DogPlatform.Logging;

public static class LoggingExtensions
{
    public static IServiceCollection AddDogPlatformHttpLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        bool persistUnhandledExceptions = true)
    {
        services.TryAddSingleton<IRequestSanitizer, RequestSanitizer>();
        services.Configure<HttpLoggingOptions>(options =>
        {
            options.CaptureRequestBody = environment.IsDevelopment();
            options.ServiceName = environment.ApplicationName;
            configuration.GetSection(HttpLoggingOptions.SectionName).Bind(options);
        });

        if (persistUnhandledExceptions)
        {
            var connectionString = configuration.GetConnectionString("IdentityDb")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:IdentityDb is required for centralized error logging.");
            services.AddSingleton<IErrorLogWriter>(new SqlErrorLogWriter(connectionString));
        }

        return services;
    }

    public static IApplicationBuilder UseDogPlatformRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();

    public static IApplicationBuilder UseDogPlatformExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
