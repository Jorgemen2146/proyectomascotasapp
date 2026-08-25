using System.Net.Http.Json;
using DogPlatform.Health.Application.Services;

namespace DogPlatform.Health.Infrastructure.Services;

public sealed class InternalPetCatalogService(HttpClient httpClient) : IInternalPetCatalogService
{
    public async Task<IReadOnlyCollection<InternalPetVaccinationContext>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            "api/v1/internal/pets/vaccination-context", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InternalPetVaccinationContext[]>(
                   cancellationToken: cancellationToken) ?? [];
    }
}
