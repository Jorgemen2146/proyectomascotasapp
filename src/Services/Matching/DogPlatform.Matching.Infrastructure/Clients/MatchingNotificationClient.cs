using System.Net.Http.Json;
using DogPlatform.Matching.Application.Clients.Notifications;
using Microsoft.Extensions.Logging;

namespace DogPlatform.Matching.Infrastructure.Clients;

public sealed class MatchingNotificationClient(
    HttpClient httpClient,
    ILogger<MatchingNotificationClient> logger) : IMatchingNotificationClient
{
    public async Task SendAsync(MatchingNotification notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "api/v1/internal/notifications", notification, cancellationToken);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Notifications rejected matching notification {Type} with {StatusCode}.",
                    notification.Type, response.StatusCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Matching notification delivery failed for {Type}; persistence is unchanged.",
                notification.Type);
        }
    }
}
