namespace DogPlatform.Matching.Domain.Enums;

/// <summary>
/// Indicates whether genealogy data could be successfully validated for a candidate.
/// </summary>
public enum GenealogyValidationStatus
{
    Validated = 0,
    Unknown = 1,
    Unavailable = 2
}
