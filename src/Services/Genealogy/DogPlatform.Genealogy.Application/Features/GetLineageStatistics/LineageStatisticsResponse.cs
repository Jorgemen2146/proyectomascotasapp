namespace DogPlatform.Genealogy.Application.Features.GetLineageStatistics;

public sealed record GenerationDistributionResponse(
    int Generation,
    int ExpectedPositions,
    int KnownPositions);

public sealed record RepeatedAncestorResponse(
    Guid AncestorPetId,
    int OccurrenceCount,
    IReadOnlyList<int> Generations,
    IReadOnlyList<string> LineagePaths,
    decimal? Contribution);

/// <summary>
/// Pedigree statistics for a pet. All numeric estimations (PedigreeCompletenessPercentage,
/// EstimatedInbreedingCoefficient) are derived exclusively from the genealogical data
/// registered in the platform; see <see cref="CalculationMethod"/> and
/// <see cref="Warnings"/> for the exact method and its limitations. They do NOT
/// constitute a veterinary or genetic diagnosis.
/// </summary>
public sealed record LineageStatisticsResponse(
    Guid PetId,
    int RequestedDepth,
    int ProcessedDepth,
    int TotalPositions,
    int KnownAncestorPositions,
    int MissingAncestorPositions,
    int UniqueAncestorCount,
    int RepeatedAncestorCount,
    decimal PedigreeCompletenessPercentage,
    IReadOnlyList<GenerationDistributionResponse> AncestorsByGeneration,
    IReadOnlyList<RepeatedAncestorResponse> RepeatedAncestors,
    decimal EstimatedInbreedingCoefficientPercentage,
    string CalculationMethod,
    IReadOnlyList<string> Warnings);
