using DogPlatform.Matching.Domain.Enums;

namespace DogPlatform.Matching.Application.Features.SearchCandidates;

public sealed record CompatibilityBreakdownResponse(
    int BreedScore,
    int AgeScore,
    int PedigreeScore,
    int GenealogyScore,
    int? HealthScore);

/// <summary>
/// Public-facing candidate summary. Never includes OwnerId, contact info, or
/// medical history.
/// </summary>
public sealed record CandidateSummaryResponse(
    Guid PetId,
    string Name,
    int BreedId,
    string BreedName,
    string Sex,
    int AgeMonths,
    string? MainPhotoUrl,
    int CompatibilityScore,
    CompatibilityBreakdownResponse CompatibilityBreakdown,
    bool HasPedigree,
    decimal? PedigreeCompletenessPercentage,
    RelationshipTypeSnapshot? RelationshipType,
    double? EstimatedOffspringInbreedingCoefficient,
    GenealogyValidationStatus GenealogyStatus,
    HealthCompatibilityStatus HealthCompatibilityStatus,
    bool IsFavorite,
    IReadOnlyList<string> Warnings,
    string SpeciesName,
    string? Color,
    string? Description,
    string RelationshipStatus,
    string? RelationshipDescription,
    IReadOnlyList<string> PhotoUrls,
    string Disclaimer);
