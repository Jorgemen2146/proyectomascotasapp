namespace DogPlatform.Matching.Domain.Enums;

/// <summary>
/// Lifecycle status of a match/cross request between two pets.
/// </summary>
public enum MatchRequestStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Cancelled = 3,
    Expired = 4
}
