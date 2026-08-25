using System.Net.Http.Json;
using DogPlatform.Notification.Application;
using Microsoft.Extensions.Logging;

namespace DogPlatform.Notification.Infrastructure;

public sealed class HealthVaccinationReminderSource(
    HttpClient httpClient,
    ILogger<HealthVaccinationReminderSource> logger) : IVaccinationReminderSource
{
    public async Task<IReadOnlyCollection<VaccinationReminderCandidate>> GetCandidatesAsync(
        DateOnly dateUtc, CancellationToken cancellationToken = default)
    {
        var path = $"api/v1/health/internal/vaccination-reminders?date={dateUtc:yyyy-MM-dd}";
        using var response = await httpClient.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Health reminder request failed with HTTP {StatusCode}.",
                (int)response.StatusCode);
            throw new HttpRequestException(
                $"Health reminder request failed with HTTP {(int)response.StatusCode}.",
                null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<VaccinationReminderCandidate[]>(
                   cancellationToken: cancellationToken) ?? [];
    }
}
