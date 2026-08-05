using DogPlatform.Matching.Domain.Enums;

namespace DogPlatform.Matching.Application.Scoring;

public sealed record CompatibilityScoreInput(
    int CandidateBreedId,
    IReadOnlyCollection<int> PreferredBreedIds,
    int CandidateAgeMonths,
    int MinimumAgeMonths,
    int MaximumAgeMonths,
    bool RequirePedigree,
    decimal? PedigreeCompletenessPercentage,
    double? EstimatedRelationshipCoefficient,
    GenealogyValidationStatus GenealogyStatus,
    double MaximumEstimatedInbreedingCoefficient,
    HealthCompatibilityStatus HealthStatus);

public sealed record CompatibilityScoreResult(
    int TotalScore,
    int BreedScore,
    int AgeScore,
    int PedigreeScore,
    int GenealogyScore,
    int? HealthScore,
    HealthCompatibilityStatus HealthStatus,
    IReadOnlyList<string> PositiveReasons,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> ExclusionReasons);
