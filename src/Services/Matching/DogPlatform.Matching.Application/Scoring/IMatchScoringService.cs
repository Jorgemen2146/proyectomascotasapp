namespace DogPlatform.Matching.Application.Scoring;

public interface IMatchScoringService
{
    /// <summary>
    /// Computes a deterministic, explainable compatibility score (0-100) between
    /// a matching profile's preferences and a candidate. Does not use randomness
    /// or AI, and never asserts medical compatibility.
    /// </summary>
    CompatibilityScoreResult Calculate(CompatibilityScoreInput input);
}
