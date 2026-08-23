using DogPlatform.Health.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Health.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddSingleton<IVaccinationScheduleService, VaccinationScheduleService>();
        services.AddSingleton<IVaccinationStatusService, VaccinationStatusService>();
        return services;
    }
}
