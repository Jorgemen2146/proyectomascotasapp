using DogPlatform.Matching.Domain.Enums;
using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Matching.Domain.Aggregates.MatchRequest;

/// <summary>
/// Records a status transition of a <see cref="MatchRequest"/> for auditing purposes.
/// Requests are never physically deleted; history preserves the full timeline.
/// </summary>
public sealed class MatchRequestStatusHistory : Entity<Guid>
{
    private MatchRequestStatusHistory(
        Guid id,
        Guid matchRequestId,
        MatchRequestStatus status,
        DateTime occurredAt)
        : base(id)
    {
        MatchRequestId = matchRequestId;
        Status = status;
        OccurredAt = occurredAt;
    }

    private MatchRequestStatusHistory() { }

    public Guid MatchRequestId { get; private set; }
    public MatchRequestStatus Status { get; private set; }
    public DateTime OccurredAt { get; private set; }

    public static MatchRequestStatusHistory Create(
        Guid matchRequestId, MatchRequestStatus status, DateTime occurredAt) =>
        new(Guid.NewGuid(), matchRequestId, status, occurredAt);
}
