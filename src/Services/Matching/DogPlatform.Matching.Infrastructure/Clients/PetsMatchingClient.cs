using System.Net;
using System.Net.Http.Json;
using DogPlatform.Matching.Application.Clients.Pets;
using Microsoft.Extensions.Logging;

namespace DogPlatform.Matching.Infrastructure.Clients;

/// <summary>
/// Typed HttpClient consuming PetsService. The BaseUrl is configured via
/// "PetsService:BaseUrl" in appsettings.
///
/// NOTE: SearchCandidatesAsync depends on a recommended-but-not-yet-implemented
/// internal endpoint: GET /api/v1/pets/internal/matching-candidates. PetsService
/// does not currently expose a way to search another owner's pets in bulk.
/// See delivery report for the exact recommended contract.
/// </summary>
public sealed class PetsMatchingClient : IPetsMatchingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PetsMatchingClient> _logger;

    public PetsMatchingClient(HttpClient httpClient, ILogger<PetsMatchingClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PetMatchingDataResponse?> GetPetForMatchingAsync(
        Guid petId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/pets/internal/{petId}/matching", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PetMatchingDataResponse>(
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error retrieving matching data for PetId={PetId}", petId);
            return null;
        }
    }

    public async Task<CandidateSearchPage?> SearchCandidatesAsync(
        CandidateSearchFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = $"api/v1/pets/internal/matching-candidates" +
                        $"?excludeOwnerId={filter.ExcludeOwnerId}" +
                        $"&sex={filter.RequiredSex}" +
                        $"&breedId={filter.BreedId}" +
                        $"&minimumAgeMonths={filter.MinimumAgeMonths}" +
                        $"&maximumAgeMonths={filter.MaximumAgeMonths}" +
                        $"&pageNumber={filter.PageNumber}" +
                        $"&pageSize={filter.PageSize}";

            var response = await _httpClient.GetAsync(query, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "PetsService candidate search returned {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CandidateSearchPage>(
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error searching matching candidates from PetsService.");
            return null;
        }
    }

    public async Task<IReadOnlyCollection<PetMatchingDataResponse>> GetPetsByIdsAsync(
        IReadOnlyCollection<Guid> petIds, CancellationToken cancellationToken = default)
    {
        if (petIds.Count == 0)
            return [];

        try
        {
            // Single batch call to avoid N+1 HTTP requests.
            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/pets/internal/matching-batch", petIds, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "PetsService batch lookup returned {StatusCode}", response.StatusCode);
                return [];
            }

            var results = await response.Content.ReadFromJsonAsync<List<PetMatchingDataResponse>>(
                cancellationToken: cancellationToken);

            return results ?? [];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error retrieving batch matching data from PetsService.");
            return [];
        }
    }

    public async Task<bool> VerifyOwnershipAsync(
        Guid petId, Guid ownerId, CancellationToken cancellationToken = default)
    {
        var pet = await GetPetForMatchingAsync(petId, cancellationToken);
        return pet is not null && pet.OwnerId == ownerId;
    }
}
