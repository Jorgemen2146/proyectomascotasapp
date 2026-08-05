using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Matching.Domain.Aggregates.FavoriteCandidate;

/// <summary>
/// Marks a candidate pet as a favorite for a given source pet's owner.
/// Favorites do not imply a match request.
/// </summary>
public sealed class FavoriteCandidate : AggregateRoot<Guid>
{
    private FavoriteCandidate(
        Guid id,
        Guid sourcePetId,
        Guid sourceOwnerId,
        Guid candidatePetId,
        DateTime createdAt)
        : base(id)
    {
        SourcePetId = sourcePetId;
        SourceOwnerId = sourceOwnerId;
        CandidatePetId = candidatePetId;
        CreatedAt = createdAt;
    }

    private FavoriteCandidate() { }

    public Guid SourcePetId { get; private set; }
    public Guid SourceOwnerId { get; private set; }
    public Guid CandidatePetId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Result<FavoriteCandidate> Create(
        Guid sourcePetId, Guid sourceOwnerId, Guid candidatePetId, DateTime utcNow)
    {
        if (sourcePetId == candidatePetId)
            return Result.Failure<FavoriteCandidate>(
                Domain.Errors.MatchingErrors.SamePet);

        return Result.Success(new FavoriteCandidate(
            Guid.NewGuid(), sourcePetId, sourceOwnerId, candidatePetId, utcNow));
    }
}
