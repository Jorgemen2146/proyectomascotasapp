using System.ComponentModel.DataAnnotations;

namespace DogPlatform.Genealogy.Application.Options;

/// <summary>
/// Configuration for the genealogical analysis features (statistics, inbreeding,
/// relationship calculation). Bound from the "GenealogyAnalysis" configuration section.
/// </summary>
public sealed class GenealogyAnalysisOptions
{
    public const string SectionName = "GenealogyAnalysis";

    /// <summary>Depth used when the client does not specify one for analysis endpoints.</summary>
    [Range(1, 100)]
    public int DefaultAnalysisDepth { get; set; } = 5;

    /// <summary>Maximum depth a client is allowed to request for analysis endpoints.</summary>
    [Range(1, 100)]
    public int MaximumAnalysisDepth { get; set; } = 10;

    /// <summary>
    /// Minimum estimated relationship coefficient (kinship) for two pets to be flagged
    /// as "close relatives" (IsCloseRelative = true).
    /// </summary>
    [Range(0, 1)]
    public double CloseRelationshipCoefficientThreshold { get; set; } = 0.125;

    /// <summary>Inbreeding coefficient at or above which a "moderate inbreeding" warning is added.</summary>
    [Range(0, 1)]
    public double InbreedingWarningThreshold { get; set; } = 0.0625;

    /// <summary>Inbreeding coefficient at or above which a "high inbreeding" warning is added.</summary>
    [Range(0, 1)]
    public double HighInbreedingThreshold { get; set; } = 0.125;
}
