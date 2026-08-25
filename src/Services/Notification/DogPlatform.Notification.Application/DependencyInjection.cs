using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IVaccinationNotificationGenerator, VaccinationNotificationGenerator>();
        services.AddScoped<IVaccinationReminderRunner, VaccinationReminderRunner>();
        return services;
    }
}
