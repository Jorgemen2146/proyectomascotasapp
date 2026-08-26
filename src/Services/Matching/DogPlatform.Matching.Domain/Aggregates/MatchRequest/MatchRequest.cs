using DogPlatform.Matching.Domain.Enums;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Matching.Domain.Aggregates.MatchRequest;

/// <summary>
/// Represents a request from the owner of one pet to connect/cross with another
/// pet, along with the compatibility snapshot at the time it was created.
/// </summary>
public sealed class MatchRequest : AggregateRoot<Guid>
{
    private readonly List<MatchRequestStatusHistory> _statusHistory = [];

    private MatchRequest(
        Guid id,
        Guid requesterPetId,
        Guid requesterOwnerId,
        Guid candidatePetId,
        Guid candidateOwnerId,
        string? message,
        int compatibilityScoreSnapshot,
        double estimatedInbreedingCoefficientSnapshot,
        RelationshipTypeSnapshot relationshipTypeSnapshot,
        bool requesterSharePhoneNumber,
        DateTime createdAt,
        DateTime? expiresAt)
        : base(id)
    {
        RequesterPetId = requesterPetId;
        RequesterOwnerId = requesterOwnerId;
        CandidatePetId = candidatePetId;
        CandidateOwnerId = candidateOwnerId;
        Status = MatchRequestStatus.Pending;
        Message = message;
        CompatibilityScoreSnapshot = compatibilityScoreSnapshot;
        EstimatedInbreedingCoefficientSnapshot = estimatedInbreedingCoefficientSnapshot;
        RelationshipTypeSnapshot = relationshipTypeSnapshot;
        RequesterSharePhoneNumber = requesterSharePhoneNumber;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;

        _statusHistory.Add(
            MatchRequestStatusHistory.Create(Id, MatchRequestStatus.Pending, createdAt));
    }

    private MatchRequest() { }

    public Guid RequesterPetId { get; private set; }
    public Guid RequesterOwnerId { get; private set; }
    public Guid CandidatePetId { get; private set; }
    public Guid CandidateOwnerId { get; private set; }
    public MatchRequestStatus Status { get; private set; }
    public string? Message { get; private set; }
    public int CompatibilityScoreSnapshot { get; private set; }
    public double EstimatedInbreedingCoefficientSnapshot { get; private set; }
    public RelationshipTypeSnapshot RelationshipTypeSnapshot { get; private set; }
    public bool RequesterSharePhoneNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public IReadOnlyCollection<MatchRequestStatusHistory> StatusHistory =>
        _statusHistory.AsReadOnly();

    public static Result<MatchRequest> Create(
        Guid requesterPetId,
        Guid requesterOwnerId,
        Guid candidatePetId,
        Guid candidateOwnerId,
        string? message,
        int compatibilityScoreSnapshot,
        double estimatedInbreedingCoefficientSnapshot,
        RelationshipTypeSnapshot relationshipTypeSnapshot,
        bool requesterSharePhoneNumber,
        DateTime utcNow,
        DateTime? expiresAt = null)
    {
        if (requesterPetId == candidatePetId)
            return Result.Failure<MatchRequest>(MatchingErrors.SamePet);

        if (message is { Length: > 500 })
            return Result.Failure<MatchRequest>(MatchingErrors.MessageTooLong);

        var request = new MatchRequest(
            Guid.NewGuid(),
            requesterPetId,
            requesterOwnerId,
            candidatePetId,
            candidateOwnerId,
            message,
            compatibilityScoreSnapshot,
            estimatedInbreedingCoefficientSnapshot,
            relationshipTypeSnapshot,
            requesterSharePhoneNumber,
            utcNow,
            expiresAt);

        return Result.Success(request);
    }

    public Result Accept(Guid respondingOwnerId, DateTime utcNow)
    {
        if (respondingOwnerId != CandidateOwnerId)
            return Result.Failure(MatchingErrors.Unauthorized);

        if (Status != MatchRequestStatus.Pending)
            return Result.Failure(MatchingErrors.RequestNotPending);

        Status = MatchRequestStatus.Accepted;
        RespondedAt = utcNow;
        UpdatedAt = utcNow;
        _statusHistory.Add(MatchRequestStatusHistory.Create(Id, Status, utcNow));

        return Result.Success();
    }

    public Result Reject(Guid respondingOwnerId, DateTime utcNow)
    {
        if (respondingOwnerId != CandidateOwnerId)
            return Result.Failure(MatchingErrors.Unauthorized);

        if (Status != MatchRequestStatus.Pending)
            return Result.Failure(MatchingErrors.RequestNotPending);

        Status = MatchRequestStatus.Rejected;
        RespondedAt = utcNow;
        UpdatedAt = utcNow;
        _statusHistory.Add(MatchRequestStatusHistory.Create(Id, Status, utcNow));

        return Result.Success();
    }

    public Result Cancel(Guid cancellingOwnerId, DateTime utcNow)
    {
        if (cancellingOwnerId != RequesterOwnerId)
            return Result.Failure(MatchingErrors.Unauthorized);

        if (Status is not MatchRequestStatus.Pending)
            return Result.Failure(MatchingErrors.RequestAlreadyFinalized);

        Status = MatchRequestStatus.Cancelled;
        CancelledAt = utcNow;
        UpdatedAt = utcNow;
        _statusHistory.Add(MatchRequestStatusHistory.Create(Id, Status, utcNow));

        return Result.Success();
    }

    public Result Expire(DateTime utcNow)
    {
        if (Status != MatchRequestStatus.Pending)
            return Result.Failure(MatchingErrors.RequestAlreadyFinalized);

        Status = MatchRequestStatus.Expired;
        UpdatedAt = utcNow;
        _statusHistory.Add(MatchRequestStatusHistory.Create(Id, Status, utcNow));

        return Result.Success();
    }
}
