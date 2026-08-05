namespace DogPlatform.Matching.Application.Features.UpsertMatchingProfile;

public sealed record MatchingProfileResponse(
    Guid MatchingProfileId,
    Guid PetId,
    bool IsActive,
    IReadOnlyList<int> PreferredBreedIds,
    int MinimumAgeMonths,
    int MaximumAgeMonths,
    bool RequirePedigree,
    bool RequireGenealogyValidation,
    double MaximumEstimatedInbreedingCoefficient,
    int MinimumCompatibilityScore,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
