using DogPlatform.Genealogy.Application.Services;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace DogPlatform.Genealogy.Infrastructure.Services;

/// <summary>
/// Verifies pet existence and ownership by calling PetsService over HTTP.
/// The base URL is configured via "PetsService:BaseUrl" in appsettings.
/// </summary>
public sealed class PetVerificationService : IPetVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PetVerificationService> _logger;

    public PetVerificationService(HttpClient httpClient, ILogger<PetVerificationService> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    public async Task<bool> PetExistsAsync(Guid petId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v1/pets/{petId}", cancellationToken);

            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking pet existence for PetId={PetId}", petId);
            return false;
        }
    }

    public async Task<bool> PetBelongsToOwnerAsync(
        Guid petId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v1/pets/{petId}", cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
                return false;

            var pet = await response.Content.ReadFromJsonAsync<PetOwnerDto>(
                cancellationToken: cancellationToken);

            return pet is not null && pet.OwnerId == ownerId;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking pet ownership for PetId={PetId}, OwnerId={OwnerId}",
                petId,
                ownerId);

            return false;
        }
    }

    // Minimal DTO — only the field we need from PetsService GET /api/v1/pets/{id}
    private sealed record PetOwnerDto(Guid OwnerId);
}
