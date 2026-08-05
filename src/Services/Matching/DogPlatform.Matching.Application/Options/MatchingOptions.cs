using System.ComponentModel.DataAnnotations;

namespace DogPlatform.Matching.Application.Options;

public sealed class MatchingWeightsOptions
{
    [Range(0, 100)]
    public int Breed { get; init; } = 35;

    [Range(0, 100)]
    public int Age { get; init; } = 20;

    [Range(0, 100)]
    public int Pedigree { get; init; } = 20;

    [Range(0, 100)]
    public int Genealogy { get; init; } = 25;

    [Range(0, 100)]
    public int Health { get; init; } = 0;

    [Range(0, 100)]
    public int Distance { get; init; } = 0;

    public int Sum => Breed + Age + Pedigree + Genealogy + Health + Distance;
}

/// <summary>
/// Configuration for the matching scoring algorithm and default exclusion rules.
/// Validated at startup: weights must be non-negative and sum to 100, thresholds
/// must be within [0,1], scores within [0,100], and ages must be valid.
/// </summary>
public sealed class MatchingOptions
{
    public const string SectionName = "Matching";

    [Range(0, 1200)]
    public int MinimumCandidateAgeMonths { get; init; } = 18;

    [Range(0, 1200)]
    public int MaximumCandidateAgeMonths { get; init; } = 96;

    [Range(0, 1)]
    public double DefaultMaximumEstimatedInbreedingCoefficient { get; init; } = 0.0625;

    [Range(0, 100)]
    public int DefaultMinimumCompatibilityScore { get; init; } = 60;

    public List<string> ExcludedRelationshipTypes { get; init; } =
    [
        "SamePet",
        "Parent",
        "Child",
        "FullSibling",
        "HalfSibling",
        "Grandparent",
        "Grandchild",
        "UncleOrAunt",
        "NephewOrNiece",
        "FirstCousin"
    ];

    public MatchingWeightsOptions Weights { get; init; } = new();

    /// <summary>Maximum candidates page size allowed regardless of client request.</summary>
    [Range(1, 200)]
    public int MaximumPageSize { get; init; } = 50;

    /// <summary>Maximum number of candidates evaluated against Genealogy per search (bounded concurrency guard).</summary>
    [Range(1, 500)]
    public int MaximumCandidatesEvaluatedPerSearch { get; init; } = 100;

    /// <summary>Bounded concurrency degree when evaluating candidates against Genealogy in parallel.</summary>
    [Range(1, 32)]
    public int GenealogyEvaluationConcurrency { get; init; } = 4;

    /// <summary>Timeout applied to outbound HTTP calls to Pets/Genealogy services.</summary>
    [Range(1, 120)]
    public int OutboundTimeoutSeconds { get; init; } = 10;
}
