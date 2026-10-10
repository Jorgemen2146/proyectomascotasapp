using System.Text.Json;
using DogPlatform.Notification.Application;
using DogPlatform.Notification.Domain.Entities;
using DogPlatform.Notification.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DogPlatform.Social.Tests;

public sealed class SocialNotificationTests
{
    [Fact]
    public async Task Persistent_event_is_stored_before_realtime_delivery()
    {
        var repository = new RecordingRepository();
        var publisher = new RecordingPublisher(repository);
        var result = await CreateHandler(repository, publisher)
            .Handle(Command("chat.message.received", true), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);
        Assert.NotNull(repository.Added);
        Assert.True(publisher.Published);
        Assert.True(publisher.RepositoryAlreadyCalled);
    }

    [Fact]
    public async Task Persistent_event_survives_realtime_failure()
    {
        var repository = new RecordingRepository();
        var result = await CreateHandler(repository, new ThrowingPublisher())
            .Handle(Command("social.friend_request.received", true), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);
        Assert.NotNull(repository.Added);
    }

    [Fact]
    public async Task Read_receipt_is_realtime_only()
    {
        var repository = new RecordingRepository();
        var publisher = new RecordingPublisher(repository);
        var result = await CreateHandler(repository, publisher)
            .Handle(Command("chat.message.read", false), default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Created);
        Assert.Equal(Guid.Empty, result.Value.NotificationId);
        Assert.Null(repository.Added);
        Assert.True(publisher.Published);
    }

    private static CreateInternalNotificationHandler CreateHandler(
        INotificationRepository repository, INotificationRealtimePublisher publisher) =>
        new(repository, publisher, TimeProvider.System,
            NullLogger<CreateInternalNotificationHandler>.Instance);

    private static CreateInternalNotificationCommand Command(string eventName, bool persist)
    {
        using var document = JsonDocument.Parse("{\"conversationId\":\"00000000-0000-0000-0000-000000000001\"}");
        return new(Guid.NewGuid(), eventName, "title", "message", "Conversation", "1",
            document.RootElement.Clone(), Guid.NewGuid().ToString("D"), persist);
    }

    private sealed class RecordingPublisher(RecordingRepository repository)
        : INotificationRealtimePublisher
    {
        public bool Published { get; private set; }
        public bool RepositoryAlreadyCalled { get; private set; }
        public Task PublishAsync(NotificationResponse notification, Guid userId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishEventAsync(Guid userId, string eventName, object data,
            CancellationToken cancellationToken = default)
        {
            Published = true;
            RepositoryAlreadyCalled = repository.Added is not null;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingPublisher : INotificationRealtimePublisher
    {
        public Task PublishAsync(NotificationResponse notification, Guid userId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishEventAsync(Guid userId, string eventName, object data,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated disconnected realtime transport.");
    }

    private sealed class RecordingRepository : INotificationRepository
    {
        public NotificationRecord? Added { get; private set; }
        public Task<NotificationInsertResult> TryAddAsync(NotificationRecord notification,
            CancellationToken cancellationToken = default)
        {
            Added = notification;
            return Task.FromResult(NotificationInsertResult.Created);
        }
        public Task<(IReadOnlyCollection<NotificationRecord> Items, int TotalCount)> GetPageAsync(
            Guid userId, int pageNumber, int pageSize, bool unreadOnly,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyCollection<NotificationRecord>)[], 0));
        public Task<int> GetUnreadCountAsync(Guid userId,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<NotificationRecord?> GetByIdAsync(Guid userId, Guid notificationId,
            CancellationToken cancellationToken = default) => Task.FromResult<NotificationRecord?>(null);
        public Task MarkAllAsReadAsync(Guid userId, DateTime utcNow,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
