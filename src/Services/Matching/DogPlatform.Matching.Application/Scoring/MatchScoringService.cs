using DogPlatform.Matching.Application.Options;
using DogPlatform.Matching.Domain.Enums;
using Microsoft.Extensions.Options;

namespace DogPlatform.Matching.Application.Scoring;

/// <summary>
/// Deterministic, explainable compatibility scoring engine. No randomness, no AI.
/// Each dimension contributes up to its configured weight; the total is the sum
/// of the dimension scores, capped between 0 and 100.
/// </summary>
public sealed class MatchScoringService : IMatchScoringService
{
    private readonly MatchingOptions _options;

    public MatchScoringService(IOptions<MatchingOptions> options)
    {
        _options = options.Value;
    }

    public CompatibilityScoreResult Calculate(CompatibilityScoreInput input)
    {
        var positiveReasons = new List<string>();
        var warnings = new List<string>();
        var exclusionReasons = new List<string>();
        var weights = _options.Weights;

        // ── Breed ──────────────────────────────────────────────────────────
        var breedMatches = input.PreferredBreedIds.Count == 0
            || input.PreferredBreedIds.Contains(input.CandidateBreedId);
        var breedScore = breedMatches ? weights.Breed : 0;
        if (breedMatches && input.PreferredBreedIds.Count > 0)
            positiveReasons.Add("Candidate breed matches preferred breeds.");
        else if (!breedMatches)
            warnings.Add("Candidate breed is not among preferred breeds.");

        // ── Age ────────────────────────────────────────────────────────────
        var withinAgeRange =
            input.CandidateAgeMonths >= input.MinimumAgeMonths &&
            input.CandidateAgeMonths <= input.MaximumAgeMonths;

        int ageScore;
        if (withinAgeRange)
        {
            var midpoint = (input.MinimumAgeMonths + input.MaximumAgeMonths) / 2.0;
            var halfRange = Math.Max(1.0, (input.MaximumAgeMonths - input.MinimumAgeMonths) / 2.0);
            var distanceFromMidpoint = Math.Abs(input.CandidateAgeMonths - midpoint);
            var proximityFactor = Math.Clamp(1.0 - (distanceFromMidpoint / halfRange), 0.0, 1.0);
            ageScore = (int)Math.Round(weights.Age * proximityFactor, MidpointRounding.AwayFromZero);
            positiveReasons.Add("Candidate age is within the preferred range.");
        }
        else
        {
            ageScore = 0;
            warnings.Add("Candidate age is outside the preferred range.");
        }

        // ── Pedigree ───────────────────────────────────────────────────────
        int pedigreeScore;
        if (input.PedigreeCompletenessPercentage is { } completeness)
        {
            var factor = (double)Math.Clamp(completeness, 0, 100) / 100.0;
            pedigreeScore = (int)Math.Round(weights.Pedigree * factor, MidpointRounding.AwayFromZero);
            if (completeness >= 75)
                positiveReasons.Add("Candidate has a highly complete pedigree.");
            else if (input.RequirePedigree)
                warnings.Add("Candidate pedigree completeness is below preferred threshold.");
        }
        else
        {
            pedigreeScore = 0;
            warnings.Add("Pedigree completeness could not be determined.");
        }

        // ── Genealogy (relationship risk / inbreeding) ────────────────────
        int genealogyScore;
        if (input.GenealogyStatus == GenealogyValidationStatus.Validated
            && input.EstimatedRelationshipCoefficient is { } coefficient)
        {
            var safeFactor = Math.Clamp(
                1.0 - (coefficient / Math.Max(input.MaximumEstimatedInbreedingCoefficient, 0.0001)),
                0.0,
                1.0);
            genealogyScore = (int)Math.Round(weights.Genealogy * safeFactor, MidpointRounding.AwayFromZero);
            positiveReasons.Add("Estimated genealogical risk is within acceptable limits.");
        }
        else
        {
            genealogyScore = 0;
            warnings.Add("Genealogy validation is unavailable or incomplete for this candidate.");
        }

        // ── Health (always neutral in v1) ─────────────────────────────────
        int? healthScore = weights.Health > 0 ? 0 : null;
        if (input.HealthStatus == HealthCompatibilityStatus.Unknown)
            warnings.Add("Health compatibility is unknown; HealthService integration is not yet available.");

        var totalScore = Math.Clamp(
            breedScore + ageScore + pedigreeScore + genealogyScore + (healthScore ?? 0),
            0,
            100);

        return new CompatibilityScoreResult(
            totalScore,
            breedScore,
            ageScore,
            pedigreeScore,
            genealogyScore,
            healthScore,
            input.HealthStatus,
            positiveReasons,
            warnings,
            exclusionReasons);
    }
}
