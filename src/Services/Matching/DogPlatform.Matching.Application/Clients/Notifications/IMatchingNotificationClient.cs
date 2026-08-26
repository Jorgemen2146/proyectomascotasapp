namespace DogPlatform.Matching.Application.Clients.Notifications;

public sealed record MatchingNotificationMetadata(
    Guid MatchId,
    Guid BreedingIntentId);

public sealed record MatchingNotification(
    Guid UserId,
    string Type,
    string Title,
    string Message,
    Guid? MatchRequestId = null,
    Guid? PetId = null,
    string? PetPhotoUrl = null,
    MatchingNotificationMetadata? Metadata = null);

public interface IMatchingNotificationClient
{
    Task SendAsync(MatchingNotification notification,
        CancellationToken cancellationToken = default);
}
