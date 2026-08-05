using DogPlatform.Matching.Application.Clients.Health;
using DogPlatform.Matching.Domain.Enums;

namespace DogPlatform.Matching.Infrastructure.Clients;

/// <summary>
/// Neutral, v1 implementation of the Health abstraction. HealthService does not
/// yet have sufficient functionality; this implementation always returns
/// Unknown and never invents genetic tests, diseases, vaccines, or a medical
/// compatibility score. Prepared for a future real integration.
/// </summary>
public sealed class HealthMatchingClient : IHealthMatchingClient
{
    private readonly TimeProvider _timeProvider;

    public HealthMatchingClient(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<HealthCompatibilityResult> EvaluateAsync(
        Guid sourcePetId, Guid candidatePetId, CancellationToken cancellationToken = default)
    {
        var result = new HealthCompatibilityResult(
            HealthCompatibilityStatus.Unknown,
            ["HealthService integration is not yet available; health compatibility is not evaluated."],
            _timeProvider.GetUtcNow().UtcDateTime);

        return Task.FromResult(result);
    }
}
