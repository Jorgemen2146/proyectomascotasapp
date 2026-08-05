namespace DogPlatform.Matching.Application.Clients.Pets;

/// <summary>
/// Abstraction over PetsService, consumed by Matching Application handlers.
/// Implemented in Infrastructure via a typed HttpClient. Handlers must never
/// call HttpClient directly.
/// </summary>
public interface IPetsMatchingClient
{
    /// <summary>Gets the minimal matching-relevant data for a single pet.</summary>
    Task<PetMatchingDataResponse?> GetPetForMatchingAsync(
        Guid petId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a preliminary, filtered page of matching candidates from PetsService.
    /// Requires PetsService to expose a batch/internal endpoint (see recommended
    /// contract: GET /api/v1/pets/internal/matching-candidates) to avoid N+1 HTTP calls.
    /// </summary>
    Task<CandidateSearchPage?> SearchCandidatesAsync(
        CandidateSearchFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Gets minimal matching data for a batch of pet ids in a single call.</summary>
    Task<IReadOnlyCollection<PetMatchingDataResponse>> GetPetsByIdsAsync(
        IReadOnlyCollection<Guid> petIds, CancellationToken cancellationToken = default);

    /// <summary>Verifies that the given pet belongs to the given owner.</summary>
    Task<bool> VerifyOwnershipAsync(
        Guid petId, Guid ownerId, CancellationToken cancellationToken = default);
}
