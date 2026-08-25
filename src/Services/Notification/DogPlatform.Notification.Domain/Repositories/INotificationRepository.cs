using DogPlatform.Notification.Domain.Entities;

namespace DogPlatform.Notification.Domain.Repositories;

public enum NotificationInsertResult
{
    Created,
    Duplicate
}

public interface INotificationRepository
{
    Task<NotificationInsertResult> TryAddAsync(NotificationRecord notification,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<NotificationRecord> Items, int TotalCount)> GetPageAsync(
        Guid userId, int pageNumber, int pageSize, bool unreadOnly,
        CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationRecord?> GetByIdAsync(Guid userId, Guid notificationId,
        CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid userId, DateTime utcNow,
        CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
