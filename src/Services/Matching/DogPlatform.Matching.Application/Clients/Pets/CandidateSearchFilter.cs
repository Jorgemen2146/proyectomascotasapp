namespace DogPlatform.Matching.Application.Clients.Pets;

/// <summary>
/// Filter criteria sent to PetsService when requesting a preliminary page of
/// candidates. PetsService is responsible for excluding the source pet's owner,
/// deleted/inactive pets, and applying sex/breed/age filters when supported.
/// </summary>
public sealed record CandidateSearchFilter(
    Guid ExcludeOwnerId,
    string? RequiredSex,
    int? BreedId,
    int MinimumAgeMonths,
    int MaximumAgeMonths,
    int PageNumber,
    int PageSize);

public sealed record CandidateSearchPage(
    IReadOnlyCollection<PetMatchingDataResponse> Items,
    int TotalItems);
