using DogPlatform.Matching.Application.Clients.Genealogy;
using DogPlatform.Matching.Application.Clients.Health;
using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Options;
using DogPlatform.Matching.Application.Scoring;
using DogPlatform.Matching.Domain.Enums;
using Microsoft.Extensions.Options;

namespace DogPlatform.Matching.Application.Evaluation;

public sealed record CandidateEvaluation(
    PetMatchingDataResponse Candidate,
    bool IsExcluded,
    string? ExclusionReason,
    CompatibilityScoreResult? Score,
    RelationshipTypeSnapshot? RelationshipType,
    double? EstimatedOffspringInbreedingCoefficient,
    decimal? PedigreeCompletenessPercentage,
    GenealogyValidationStatus GenealogyStatus,
    HealthCompatibilityResult Health,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Shared candidate evaluation logic reused by SearchCandidates, GetCandidateDetail,
/// and CreateMatchRequest. Applies exclusion rules, consults Genealogy/Health,
/// and computes the deterministic compatibility score.
/// </summary>
public sealed class CandidateEvaluationService
{
    private readonly IGenealogyMatchingClient _genealogyClient;
    private readonly IHealthMatchingClient _healthClient;
    private readonly IMatchScoringService _scoringService;
    private readonly MatchingOptions _options;

    public CandidateEvaluationService(
        IGenealogyMatchingClient genealogyClient,
        IHealthMatchingClient healthClient,
        IMatchScoringService scoringService,
        IOptions<MatchingOptions> options)
    {
        _genealogyClient = genealogyClient;
        _healthClient = healthClient;
        _scoringService = scoringService;
        _options = options.Value;
    }

    public async Task<CandidateEvaluation> EvaluateAsync(
        PetMatchingDataResponse sourcePet,
        PetMatchingDataResponse candidate,
        Domain.Aggregates.MatchingProfile.MatchingProfile sourceProfile,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();

        // ── Basic exclusion rules ────────────────────────────────────────
        if (candidate.PetId == sourcePet.PetId)
            return Excluded(candidate, "SamePet");

        if (candidate.OwnerId == sourcePet.OwnerId)
            return Excluded(candidate, "SameOwner");

        if (candidate.IsDeleted || !candidate.IsActive)
            return Excluded(candidate, "CandidateNotActiveOrDeleted");

        if (candidate.IsSterilized)
            return Excluded(candidate, "CandidateSterilized");

        if (sourcePet.SpeciesId != 0 && candidate.SpeciesId != 0
            && sourcePet.SpeciesId != candidate.SpeciesId)
            return Excluded(candidate, "DifferentSpecies");

        var requiredSex = sourceProfile.LookingForSex
            ?? (string.Equals(sourcePet.Sex, "M", StringComparison.OrdinalIgnoreCase) ? "F" : "M");
        if (!string.Equals(candidate.Sex, requiredSex, StringComparison.OrdinalIgnoreCase))
            return Excluded(candidate, "SameSex");

        if (candidate.AgeMonths < sourceProfile.MinimumAgeMonths
            || candidate.AgeMonths > sourceProfile.MaximumAgeMonths)
            return Excluded(candidate, "OutOfAgeRange");

        var preferredBreedIds = sourceProfile.BreedPreferences.Select(bp => bp.BreedId).ToList();
        if (!sourceProfile.AllowMixedBreed && preferredBreedIds.Count > 0
            && !preferredBreedIds.Contains(candidate.BreedId))
            return Excluded(candidate, "BreedNotPreferred");

        // ── Genealogy ────────────────────────────────────────────────────
        var relationship = await _genealogyClient.CalculateRelationshipAsync(
            sourcePet.PetId, candidate.PetId, cancellationToken);

        GenealogyValidationStatus genealogyStatus;
        RelationshipTypeSnapshot? relationshipType = null;
        double? relationshipCoefficient = null;

        if (relationship is null)
        {
            genealogyStatus = GenealogyValidationStatus.Unavailable;
            warnings.Add("Genealogy service is unavailable for this candidate.");

            if (sourceProfile.RequireGenealogyValidation)
                return Excluded(candidate, "GenealogyValidationRequiredButUnavailable");
        }
        else
        {
            genealogyStatus = relationship.Status;
            relationshipType = relationship.RelationshipType;
            relationshipCoefficient = relationship.EstimatedRelationshipCoefficient;
            warnings.AddRange(relationship.Warnings);

            if (_options.ExcludedRelationshipTypes.Contains(relationship.RelationshipType.ToString()))
                return Excluded(candidate, $"ExcludedRelationshipType:{relationship.RelationshipType}");
        }

        // ── Offspring inbreeding estimate ────────────────────────────────
        double? estimatedOffspringInbreeding = null;
        var offspringEstimate = await _genealogyClient.EstimateOffspringInbreedingAsync(
            sourcePet.PetId, candidate.PetId, cancellationToken);

        if (offspringEstimate is not null)
        {
            estimatedOffspringInbreeding = offspringEstimate.EstimatedOffspringInbreedingCoefficient;
            warnings.AddRange(offspringEstimate.Warnings);

            if (estimatedOffspringInbreeding > sourceProfile.MaximumEstimatedInbreedingCoefficient)
                return Excluded(candidate, "ExceedsMaximumEstimatedInbreedingCoefficient");
        }
        else if (sourceProfile.RequireGenealogyValidation)
        {
            return Excluded(candidate, "InbreedingEstimateUnavailable");
        }

        // ── Pedigree completeness ───────────────────────────────────────
        decimal? pedigreeCompleteness = null;
        var pedigreeStats = await _genealogyClient.GetPedigreeStatisticsAsync(
            candidate.PetId, cancellationToken);

        if (pedigreeStats is not null)
        {
            pedigreeCompleteness = pedigreeStats.PedigreeCompletenessPercentage;
            warnings.AddRange(pedigreeStats.Warnings);
        }
        else if (sourceProfile.RequirePedigree)
        {
            warnings.Add("Pedigree statistics could not be determined for this candidate.");
        }

        // ── Health (always neutral in v1) ───────────────────────────────
        var health = await _healthClient.EvaluateAsync(sourcePet.PetId, candidate.PetId, cancellationToken);
        warnings.AddRange(health.Warnings);

        // ── Score ────────────────────────────────────────────────────────
        var scoreInput = new CompatibilityScoreInput(
            candidate.BreedId,
            preferredBreedIds,
            candidate.AgeMonths,
            sourceProfile.MinimumAgeMonths,
            sourceProfile.MaximumAgeMonths,
            sourceProfile.RequirePedigree,
            pedigreeCompleteness,
            relationshipCoefficient,
            genealogyStatus,
            sourceProfile.MaximumEstimatedInbreedingCoefficient,
            health.Status);

        var score = _scoringService.Calculate(scoreInput);
        warnings.AddRange(score.Warnings);

        if (score.TotalScore < sourceProfile.MinimumCompatibilityScore)
            return Excluded(candidate, "BelowMinimumCompatibilityScore");

        warnings.Add("La compatibilidad mostrada no reemplaza una evaluación veterinaria.");

        return new CandidateEvaluation(
            candidate,
            false,
            null,
            score,
            relationshipType,
            estimatedOffspringInbreeding,
            pedigreeCompleteness,
            genealogyStatus,
            health,
            warnings);
    }

    private static CandidateEvaluation Excluded(PetMatchingDataResponse candidate, string reason) =>
        new(candidate, true, reason, null, null, null, null,
            GenealogyValidationStatus.Unknown,
            new HealthCompatibilityResult(HealthCompatibilityStatus.Unknown, [], DateTime.UtcNow),
            []);
}
