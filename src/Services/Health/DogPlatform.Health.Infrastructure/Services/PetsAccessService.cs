using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DogPlatform.Health.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DogPlatform.Health.Infrastructure.Services;

public sealed class PetsAccessService : IPetAccessService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PetsAccessService> _logger;
    public PetsAccessService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<PetsAccessService> logger)
        => (_httpClient, _httpContextAccessor, _logger) = (httpClient, httpContextAccessor, logger);

    public async Task<PetAccessResult> GetAccessiblePetAsync(Guid petId, CancellationToken cancellationToken = default)
    {
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization) || !AuthenticationHeaderValue.TryParse(authorization, out var header))
            return PetAccessResult.Unauthenticated();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/pets/{petId:D}/health-context");
        request.Headers.Authorization = header;
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return PetAccessResult.NotFound();
            if (response.StatusCode == HttpStatusCode.Forbidden)
                return PetAccessResult.Forbidden();
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return PetAccessResult.Unauthenticated();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pets ownership verification returned {StatusCode} for PetId={PetId}", response.StatusCode, petId);
                return PetAccessResult.Unavailable();
            }
            var pet = await response.Content.ReadFromJsonAsync<PetResponse>(cancellationToken: cancellationToken);
            return pet is null || pet.PetId != petId
                ? PetAccessResult.Unavailable()
                : PetAccessResult.Accessible(new PetHealthData(pet.PetId, pet.SpeciesId, pet.BirthDate, pet.Name));
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Pets ownership verification timed out for PetId={PetId}", petId);
            return PetAccessResult.Unavailable();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Pets ownership verification failed for PetId={PetId}", petId);
            return PetAccessResult.Unavailable();
        }
    }

    private sealed record PetResponse(Guid PetId, int SpeciesId, DateTime? BirthDate, string Name);
}
