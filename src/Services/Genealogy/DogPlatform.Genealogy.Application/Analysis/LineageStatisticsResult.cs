namespace DogPlatform.Genealogy.Application.Analysis;

/// <summary>Ancestor occupancy for one generation of a pet's pedigree.</summary>
/// <param name="Generation">1 = parents, 2 = grandparents, etc.</param>
/// <param name="ExpectedPositions">2^Generation.</param>
/// <param name="KnownPositions">Number of those positions with a recorded pet.</param>
/// <param name="AncestorIds">The known ancestor ids occupying this generation (may repeat).</param>
public sealed record GenerationDistribution(
    int Generation,
    int ExpectedPositions,
    int KnownPositions,
    IReadOnlyList<Guid> AncestorIds);

/// <summary>An ancestor that occupies more than one position in the pedigree tree.</summary>
/// <param name="AncestorPetId">The repeated ancestor.</param>
/// <param name="OccurrenceCount">Total number of positions it occupies.</param>
/// <param name="Generations">Every generation number in which it appears (may repeat itself if in more than one position per generation).</param>
/// <param name="LineagePaths">Every distinct father/mother path that reaches this ancestor.</param>
/// <param name="Contribution">
/// This ancestor's contribution to the pet's estimated inbreeding coefficient
/// (sum of (1/2)^(n1+n2+1) * (1+F_A) terms attributable to it), or null when the pet's
/// own inbreeding coefficient could not be computed (e.g. incomplete parent data).
/// </param>
public sealed record RepeatedAncestor(
    Guid AncestorPetId,
    int OccurrenceCount,
    IReadOnlyList<int> Generations,
    IReadOnlyList<string> LineagePaths,
    decimal? Contribution);

/// <summary>Full lineage statistics result for a single pet.</summary>
public sealed record LineageStatisticsResult(
    Guid PetId,
    int RequestedDepth,
    int ProcessedDepth,
    int TotalPositions,
    int KnownAncestorPositions,
    int MissingAncestorPositions,
    int UniqueAncestorCount,
    int RepeatedAncestorCount,
    decimal PedigreeCompletenessPercentage,
    IReadOnlyList<GenerationDistribution> AncestorsByGeneration,
    IReadOnlyList<RepeatedAncestor> RepeatedAncestors,
    decimal EstimatedInbreedingCoefficientPercentage,
    string CalculationMethod,
    IReadOnlyList<string> Warnings);
