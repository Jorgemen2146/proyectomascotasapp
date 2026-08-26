using DogPlatform.Matching.Domain.Enums;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Matching.Domain.Aggregates.BreedingIntent;

public sealed class BreedingIntent : AggregateRoot<Guid>
{
    private BreedingIntent(Guid id, Guid matchId, Guid proposerOwnerId,
        string? notes, DateTime? expectedDateUtc, DateTime createdAtUtc) : base(id)
    {
        MatchId = matchId;
        OpenMatchId = matchId;
        ProposerOwnerId = proposerOwnerId;
        Notes = notes;
        ExpectedDateUtc = expectedDateUtc;
        Status = BreedingIntentStatus.Proposed;
        CreatedAtUtc = createdAtUtc;
    }

    private BreedingIntent() { }

    public Guid MatchId { get; private set; }
    public Guid? OpenMatchId { get; private set; }
    public Guid ProposerOwnerId { get; private set; }
    public BreedingIntentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? ExpectedDateUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public static Result<BreedingIntent> Create(Guid matchId, Guid proposerOwnerId,
        string? notes, DateTime? expectedDateUtc, DateTime utcNow)
    {
        if (notes is { Length: > 1000 })
            return Result.Failure<BreedingIntent>(MatchingErrors.BreedingIntentNotesTooLong);
        return Result.Success(new BreedingIntent(Guid.NewGuid(), matchId,
            proposerOwnerId, notes?.Trim(), expectedDateUtc, utcNow));
    }

    public Result Accept(Guid acceptingOwnerId, Guid owner1Id, Guid owner2Id, DateTime utcNow)
    {
        if (Status != BreedingIntentStatus.Proposed)
            return Result.Failure(MatchingErrors.RequestAlreadyProcessed);
        if (acceptingOwnerId == ProposerOwnerId
            || (acceptingOwnerId != owner1Id && acceptingOwnerId != owner2Id))
            return Result.Failure(MatchingErrors.Forbidden);
        Status = BreedingIntentStatus.Agreed;
        AcceptedAtUtc = utcNow;
        return Result.Success();
    }

    public Result Cancel(Guid userId, Guid owner1Id, Guid owner2Id, DateTime utcNow)
    {
        if (Status is not (BreedingIntentStatus.Proposed or BreedingIntentStatus.Agreed))
            return Result.Failure(MatchingErrors.RequestAlreadyProcessed);
        if (userId != owner1Id && userId != owner2Id)
            return Result.Failure(MatchingErrors.Forbidden);
        if (Status == BreedingIntentStatus.Proposed && userId != ProposerOwnerId)
            return Result.Failure(MatchingErrors.Forbidden);
        Status = BreedingIntentStatus.Cancelled;
        OpenMatchId = null;
        CancelledAtUtc = utcNow;
        return Result.Success();
    }
}
