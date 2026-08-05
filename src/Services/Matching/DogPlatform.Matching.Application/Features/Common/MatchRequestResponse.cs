using DogPlatform.Matching.Domain.Enums;

namespace DogPlatform.Matching.Application.Features.Common;

public sealed record MatchRequestResponse(
    Guid MatchRequestId,
    Guid RequesterPetId,
    Guid CandidatePetId,
    MatchRequestStatus Status,
    string? Message,
    int CompatibilityScoreSnapshot,
    double EstimatedInbreedingCoefficientSnapshot,
    RelationshipTypeSnapshot RelationshipTypeSnapshot,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? RespondedAt,
    DateTime? CancelledAt,
    DateTime? ExpiresAt);
