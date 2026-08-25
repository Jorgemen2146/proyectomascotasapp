using DogPlatform.Notification.Application;
using DogPlatform.Notification.Domain.Repositories;
using DogPlatform.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotificationsDb")
            ?? throw new InvalidOperationException("ConnectionStrings:NotificationsDb is required.");
        var healthBaseUrl = configuration["HealthService:BaseUrl"]
            ?? throw new InvalidOperationException("HealthService:BaseUrl is required.");
        var internalKey = configuration["InternalServices:ApiKey"];
        if (string.IsNullOrWhiteSpace(internalKey))
            throw new InvalidOperationException("InternalServices:ApiKey is required.");

        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddHttpClient<IVaccinationReminderSource, HealthVaccinationReminderSource>(client =>
        {
            client.BaseAddress = new Uri(healthBaseUrl);
            client.DefaultRequestHeaders.Add("X-DogPlatform-Internal-Key", internalKey);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<ICurrentUser, CurrentUserService>();
        return services;
    }
}
