using System.Net;
using System.Net.Http.Json;
using DogPlatform.Matching.Application.Clients.Identity;
using Microsoft.Extensions.Logging;

namespace DogPlatform.Matching.Infrastructure.Clients;

public sealed class IdentityMatchingClient(
    HttpClient httpClient,
    ILogger<IdentityMatchingClient> logger) : IIdentityMatchingClient
{
    public async Task<MatchingOwnerContact?> GetMatchingContactAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"api/v1/internal/identity/users/{userId}/matching-contact", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Identity matching-contact lookup returned {StatusCode}.", response.StatusCode);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<MatchingOwnerContact>(
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Identity matching-contact lookup failed.");
            return null;
        }
    }
}
