namespace DogPlatform.Matching.API.Requests;

public sealed record UpsertMatchingProfileRequest(
    bool IsActive,
    IReadOnlyList<int> PreferredBreedIds,
    int MinimumAgeMonths,
    int MaximumAgeMonths,
    bool RequirePedigree,
    bool RequireGenealogyValidation,
    double MaximumEstimatedInbreedingCoefficient,
    int MinimumCompatibilityScore);
