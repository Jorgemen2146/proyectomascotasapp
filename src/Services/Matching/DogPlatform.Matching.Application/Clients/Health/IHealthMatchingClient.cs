using DogPlatform.Matching.Domain.Enums;

namespace DogPlatform.Matching.Application.Clients.Health;

/// <summary>
/// Future abstraction for HealthService integration. In Matching v1 this is
/// always neutral (Unknown) — no medical tests, diseases, vaccines, or clinical
/// compatibility are inferred or invented.
/// </summary>
public sealed record HealthCompatibilityResult(
    HealthCompatibilityStatus Status,
    IReadOnlyList<string> Warnings,
    DateTime EvaluatedAt);

public interface IHealthMatchingClient
{
    Task<HealthCompatibilityResult> EvaluateAsync(
        Guid sourcePetId, Guid candidatePetId, CancellationToken cancellationToken = default);
}
