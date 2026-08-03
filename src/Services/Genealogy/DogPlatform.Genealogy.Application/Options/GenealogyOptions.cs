using System.ComponentModel.DataAnnotations;

namespace DogPlatform.Genealogy.Application.Options;

/// <summary>
/// Configuration for genealogy tree traversal (ancestors, descendants).
/// Bound from the "Genealogy" configuration section.
/// </summary>
public sealed class GenealogyOptions
{
    public const string SectionName = "Genealogy";

    /// <summary>Depth used when the client does not specify one.</summary>
    [Range(1, 100)]
    public int DefaultTreeDepth { get; set; } = 3;

    /// <summary>Maximum depth a client is allowed to request.</summary>
    [Range(1, 100)]
    public int MaximumTreeDepth { get; set; } = 10;

    /// <summary>Safety cap on the total number of nodes visited during a single traversal.</summary>
    [Range(1, 100_000)]
    public int MaximumTraversalNodes { get; set; } = 1000;
}
