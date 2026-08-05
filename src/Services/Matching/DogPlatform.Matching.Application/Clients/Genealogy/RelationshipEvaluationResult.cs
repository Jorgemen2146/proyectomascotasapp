using DogPlatform.Matching.Domain.Enums;

namespace DogPlatform.Matching.Application.Clients.Genealogy;

/// <summary>
/// Public genealogy data about a candidate relationship, as returned by
/// GenealogyService v3. Matching never recalculates this itself and never
/// exposes the full ancestor tree.
/// </summary>
public sealed record RelationshipEvaluationResult(
    RelationshipTypeSnapshot RelationshipType,
    bool IsCloseRelative,
    double EstimatedRelationshipCoefficient,
    GenealogyValidationStatus Status,
    IReadOnlyList<string> Warnings);

public sealed record OffspringInbreedingEstimate(
    double EstimatedOffspringInbreedingCoefficient,
    GenealogyValidationStatus Status,
    IReadOnlyList<string> Warnings);

public sealed record PedigreeStatisticsSummary(
    decimal PedigreeCompletenessPercentage,
    GenealogyValidationStatus Status,
    IReadOnlyList<string> Warnings);
