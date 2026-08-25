using DogPlatform.Notification.Domain.Entities;
using DogPlatform.Notification.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Notification.Infrastructure.Persistence;

public sealed class NotificationRepository(NotificationsDbContext context) : INotificationRepository
{
    public async Task<NotificationInsertResult> TryAddAsync(
        NotificationRecord notification, CancellationToken cancellationToken = default)
    {
        await context.Notifications.AddAsync(notification, cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return NotificationInsertResult.Created;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            context.Entry(notification).State = EntityState.Detached;
            return NotificationInsertResult.Duplicate;
        }
    }

    public async Task<(IReadOnlyCollection<NotificationRecord> Items, int TotalCount)> GetPageAsync(
        Guid userId, int pageNumber, int pageSize, bool unreadOnly,
        CancellationToken cancellationToken = default)
    {
        var query = context.Notifications.AsNoTracking().Where(x => x.UserId == userId);
        if (unreadOnly)
            query = query.Where(x => !x.IsRead);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return (items, total);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);

    public Task<NotificationRecord?> GetByIdAsync(
        Guid userId, Guid notificationId, CancellationToken cancellationToken = default) =>
        context.Notifications.FirstOrDefaultAsync(
            x => x.UserId == userId && x.NotificationId == notificationId, cancellationToken);

    public async Task MarkAllAsReadAsync(
        Guid userId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        await context.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsRead, true)
                .SetProperty(x => x.ReadAtUtc, utcNow), cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
