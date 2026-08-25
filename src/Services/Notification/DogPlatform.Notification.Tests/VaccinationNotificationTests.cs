using System.Security.Claims;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DogPlatform.Notification.API.WebSockets;
using DogPlatform.Notification.Application;
using DogPlatform.Notification.Domain.Entities;
using DogPlatform.Notification.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DogPlatform.Notification.Tests;

public sealed class VaccinationNotificationTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime Now = new(2026, 8, 23, 13, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("DueSoon", true, "VaccinationDueSoon")]
    [InlineData("DueToday", true, "VaccinationDueToday")]
    [InlineData("Overdue", true, "VaccinationOverdue")]
    [InlineData("NotStarted", true, "VaccinationNotStarted")]
    public async Task Actionable_status_generates_notification(
        string status, bool eligible, string expectedType)
    {
        var fixture = new Fixture();
        var result = await fixture.Generator.GenerateAsync([Candidate(status, eligible)]);

        Assert.Equal(1, result.CreatedCount);
        var notification = Assert.Single(fixture.Repository.Items);
        Assert.Equal(expectedType, notification.Type.ToString());
        Assert.Equal(UserId, notification.UserId);
        Assert.Single(fixture.Publisher.Published);
    }

    [Fact]
    public async Task Ineligible_not_started_does_not_generate_notification()
    {
        var fixture = new Fixture();
        var result = await fixture.Generator.GenerateAsync([Candidate("NotStarted", false)]);
        Assert.Equal(0, result.CreatedCount);
        Assert.Empty(fixture.Repository.Items);
    }

    [Fact]
    public async Task Up_to_date_does_not_generate_notification()
    {
        var fixture = new Fixture();
        var result = await fixture.Generator.GenerateAsync([Candidate("UpToDate", true)]);
        Assert.Equal(0, result.CreatedCount);
        Assert.Empty(fixture.Repository.Items);
    }

    [Fact]
    public async Task Second_execution_on_same_day_is_duplicate()
    {
        var fixture = new Fixture();
        await fixture.Generator.GenerateAsync([Candidate("DueSoon", true)]);
        var second = await fixture.Generator.GenerateAsync([Candidate("DueSoon", true)]);
        Assert.Equal(0, second.CreatedCount);
        Assert.Equal(1, second.DuplicateCount);
        Assert.Single(fixture.Repository.Items);
    }

    [Fact]
    public async Task Next_day_allows_new_notification()
    {
        var fixture = new Fixture();
        await fixture.Generator.GenerateAsync([Candidate("Overdue", true)]);
        fixture.Time.UtcNow = Now.AddDays(1);
        var nextDay = await fixture.Generator.GenerateAsync([Candidate("Overdue", true)]);
        Assert.Equal(1, nextDay.CreatedCount);
        Assert.Equal(2, fixture.Repository.Items.Count);
    }

    [Fact]
    public async Task Unread_count_uses_authenticated_user_scope()
    {
        var fixture = new Fixture();
        await fixture.Generator.GenerateAsync([Candidate("DueToday", true)]);
        fixture.Repository.Items.Add(CreateFor(Guid.NewGuid(), "other-key"));
        var handler = new GetUnreadCountQueryHandler(fixture.Repository);
        var result = await handler.Handle(new GetUnreadCountQuery(UserId), default);
        Assert.Equal(1, result.Value.Count);
    }

    [Fact]
    public async Task Mark_read_only_finds_owner_notification()
    {
        var fixture = new Fixture();
        var owned = CreateFor(UserId, "owned");
        var foreign = CreateFor(Guid.NewGuid(), "foreign");
        fixture.Repository.Items.AddRange([owned, foreign]);
        var handler = new MarkNotificationReadCommandHandler(fixture.Repository, fixture.Time);

        var denied = await handler.Handle(new(UserId, foreign.NotificationId), default);
        var success = await handler.Handle(new(UserId, owned.NotificationId), default);

        Assert.True(denied.IsFailure);
        Assert.True(success.IsSuccess);
        Assert.True(owned.IsRead);
        Assert.False(foreign.IsRead);
    }

    [Fact]
    public async Task Mark_all_read_only_updates_current_user()
    {
        var fixture = new Fixture();
        var first = CreateFor(UserId, "first");
        var second = CreateFor(UserId, "second");
        var foreign = CreateFor(Guid.NewGuid(), "foreign");
        fixture.Repository.Items.AddRange([first, second, foreign]);
        var handler = new MarkAllNotificationsReadCommandHandler(fixture.Repository, fixture.Time);

        await handler.Handle(new(UserId), default);

        Assert.True(first.IsRead);
        Assert.True(second.IsRead);
        Assert.False(foreign.IsRead);
    }

    [Fact]
    public async Task Health_failure_does_not_invoke_generator()
    {
        var generator = new TrackingGenerator();
        var runner = new VaccinationReminderRunner(new FailingSource(), generator,
            new MutableTimeProvider(Now));
        await Assert.ThrowsAsync<HttpRequestException>(() => runner.RunAsync());
        Assert.Equal(0, generator.CallCount);
    }

    [Fact]
    public async Task Realtime_failure_does_not_rollback_persisted_notification()
    {
        var repository = new FakeRepository();
        var generator = new VaccinationNotificationGenerator(repository, new ThrowingPublisher(),
            new MutableTimeProvider(Now), NullLogger<VaccinationNotificationGenerator>.Instance);

        var result = await generator.GenerateAsync([Candidate("DueToday", true)]);

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task List_notifications_keeps_pagination_and_authenticated_user_scope()
    {
        var fixture = new Fixture();
        fixture.Repository.Items.AddRange([
            CreateFor(UserId, "owned"),
            CreateFor(Guid.NewGuid(), "foreign")
        ]);
        var handler = new ListNotificationsQueryHandler(fixture.Repository);

        var result = await handler.Handle(new(UserId, 1, 20, false), default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task Reminder_runner_continues_to_invoke_source_and_generator()
    {
        var generator = new TrackingGenerator();
        var runner = new VaccinationReminderRunner(new SuccessfulSource(), generator,
            new MutableTimeProvider(Now));

        await runner.RunAsync();

        Assert.Equal(1, generator.CallCount);
    }

    private static VaccinationReminderCandidate Candidate(string status, bool eligible) =>
        new(UserId, PetId, "Andrea Kitty", 1, "Rabia", status, eligible,
            null, Now.AddDays(3), 3, status == "Overdue" ? 4 : null);

    private static NotificationRecord CreateFor(Guid userId, string key) =>
        NotificationRecord.CreateVaccination(userId, PetId, 1,
            DogPlatform.Notification.Domain.Enums.NotificationType.VaccinationDueSoon, "Title", "Message",
            Now, DateOnly.FromDateTime(Now), key, "{}");

    private sealed class Fixture
    {
        public FakeRepository Repository { get; } = new();
        public FakePublisher Publisher { get; } = new();
        public MutableTimeProvider Time { get; } = new(Now);
        public VaccinationNotificationGenerator Generator => new(
            Repository, Publisher, Time, NullLogger<VaccinationNotificationGenerator>.Instance);
    }

    private sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider
    {
        public DateTime UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => new(UtcNow);
    }

    private sealed class FakePublisher : INotificationRealtimePublisher
    {
        public List<(Guid UserId, NotificationResponse Notification)> Published { get; } = [];
        public Task PublishAsync(NotificationResponse notification, Guid userId,
            CancellationToken cancellationToken = default)
        {
            Published.Add((userId, notification));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingPublisher : INotificationRealtimePublisher
    {
        public Task PublishAsync(NotificationResponse notification, Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new WebSocketException("Connection unavailable");
    }

    private sealed class FakeRepository : INotificationRepository
    {
        public List<NotificationRecord> Items { get; } = [];
        private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

        public Task<NotificationInsertResult> TryAddAsync(NotificationRecord notification,
            CancellationToken cancellationToken = default)
        {
            if (!_keys.Add(notification.DeduplicationKey))
                return Task.FromResult(NotificationInsertResult.Duplicate);
            Items.Add(notification);
            return Task.FromResult(NotificationInsertResult.Created);
        }

        public Task<(IReadOnlyCollection<NotificationRecord> Items, int TotalCount)> GetPageAsync(
            Guid userId, int pageNumber, int pageSize, bool unreadOnly,
            CancellationToken cancellationToken = default)
        {
            var query = Items.Where(x => x.UserId == userId && (!unreadOnly || !x.IsRead)).ToArray();
            return Task.FromResult<(IReadOnlyCollection<NotificationRecord>, int)>(
                (query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray(), query.Length));
        }

        public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Count(x => x.UserId == userId && !x.IsRead));

        public Task<NotificationRecord?> GetByIdAsync(Guid userId, Guid notificationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.UserId == userId && x.NotificationId == notificationId));

        public Task MarkAllAsReadAsync(Guid userId, DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            foreach (var notification in Items.Where(x => x.UserId == userId))
                notification.MarkAsRead(utcNow);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FailingSource : IVaccinationReminderSource
    {
        public Task<IReadOnlyCollection<VaccinationReminderCandidate>> GetCandidatesAsync(
            DateOnly dateUtc, CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("Health unavailable");
    }

    private sealed class SuccessfulSource : IVaccinationReminderSource
    {
        public Task<IReadOnlyCollection<VaccinationReminderCandidate>> GetCandidatesAsync(
            DateOnly dateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<VaccinationReminderCandidate>>(
                [Candidate("DueSoon", true)]);
    }

    private sealed class TrackingGenerator : IVaccinationNotificationGenerator
    {
        public int CallCount { get; private set; }
        public Task<VaccinationReminderRunResult> GenerateAsync(
            IReadOnlyCollection<VaccinationReminderCandidate> candidates,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new VaccinationReminderRunResult(0, 0, 0, 0));
        }
    }
}

public sealed class NotificationWebSocketTests
{
    private static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Authenticated_connection_uses_user_id_from_claim_and_is_removed_on_close()
    {
        var socket = new RecordingWebSocket(closeOnReceive: true);
        var feature = new TestWebSocketFeature(socket);
        var manager = new RecordingConnectionManager();
        var context = AuthenticatedContext(UserId);
        context.Features.Set<IHttpWebSocketFeature>(feature);

        await NotificationWebSocketEndpoint.HandleAsync(context, manager);

        Assert.True(feature.WasAccepted);
        Assert.Equal(UserId, Assert.Single(manager.Added).UserId);
        Assert.Equal(UserId, Assert.Single(manager.Removed).UserId);
    }

    [Fact]
    public async Task Unauthenticated_connection_is_rejected()
    {
        var context = new DefaultHttpContext();
        var manager = new RecordingConnectionManager();

        await NotificationWebSocketEndpoint.HandleAsync(context, manager);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Empty(manager.Added);
    }

    [Fact]
    public async Task Normal_http_request_is_rejected_with_bad_request()
    {
        var context = AuthenticatedContext(UserId);

        await NotificationWebSocketEndpoint.HandleAsync(context, new RecordingConnectionManager());

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task Multiple_connections_for_same_user_receive_payload()
    {
        var manager = CreateManager();
        var first = new RecordingWebSocket();
        var second = new RecordingWebSocket();
        await manager.AddAsync(UserId, first);
        await manager.AddAsync(UserId, second);

        await manager.SendToUserAsync(UserId, "hello"u8.ToArray());

        Assert.Single(first.Messages);
        Assert.Single(second.Messages);
        Assert.Equal(2, manager.GetConnectionCount(UserId));
    }

    [Fact]
    public async Task Payload_is_delivered_only_to_target_user()
    {
        var manager = CreateManager();
        var target = new RecordingWebSocket();
        var foreign = new RecordingWebSocket();
        await manager.AddAsync(UserId, target);
        await manager.AddAsync(Guid.NewGuid(), foreign);

        await manager.SendToUserAsync(UserId, "private"u8.ToArray());

        Assert.Single(target.Messages);
        Assert.Empty(foreign.Messages);
    }

    [Fact]
    public async Task Closed_connection_is_removed_automatically()
    {
        var manager = CreateManager();
        var socket = new RecordingWebSocket();
        await manager.AddAsync(UserId, socket);
        socket.SetState(WebSocketState.Closed);

        await manager.SendToUserAsync(UserId, "ignored"u8.ToArray());

        Assert.Equal(0, manager.GetConnectionCount(UserId));
    }

    [Fact]
    public async Task Publisher_emits_stable_notification_envelope()
    {
        var manager = new RecordingConnectionManager();
        var publisher = new WebSocketNotificationRealtimePublisher(
            manager, Options.Create(new JsonOptions()));
        var notificationId = Guid.NewGuid();
        var notification = new NotificationResponse(notificationId, "VaccinationDueSoon",
            "Title", "Message", Guid.NewGuid(), 1, "Created", false, null,
            DateTime.UtcNow, "{}");

        await publisher.PublishAsync(notification, UserId);

        using var document = JsonDocument.Parse(Assert.Single(manager.Payloads));
        Assert.Equal("notificationReceived", document.RootElement.GetProperty("event").GetString());
        var data = document.RootElement.GetProperty("data");
        Assert.Equal(notificationId, data.GetProperty("notificationId").GetGuid());
        Assert.Equal("VaccinationDueSoon", data.GetProperty("type").GetString());
        Assert.Equal("Title", data.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Concurrent_add_remove_and_send_operations_do_not_fail()
    {
        var manager = CreateManager();
        var sockets = Enumerable.Range(0, 20).Select(_ => new RecordingWebSocket()).ToArray();

        var tasks = sockets.Select(async socket =>
        {
            await manager.AddAsync(UserId, socket);
            await manager.SendToUserAsync(UserId, "event"u8.ToArray());
            await manager.RemoveAsync(UserId, socket);
        });

        await Task.WhenAll(tasks);
        Assert.Equal(0, manager.GetConnectionCount(UserId));
    }

    private static NotificationWebSocketConnectionManager CreateManager() =>
        new(NullLogger<NotificationWebSocketConnectionManager>.Instance);

    private static DefaultHttpContext AuthenticatedContext(Guid userId) => new()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", userId.ToString("D"))], "test"))
    };

    private sealed class RecordingConnectionManager : INotificationWebSocketConnectionManager
    {
        public List<(Guid UserId, WebSocket Socket)> Added { get; } = [];
        public List<(Guid UserId, WebSocket Socket)> Removed { get; } = [];
        public List<byte[]> Payloads { get; } = [];

        public Task AddAsync(Guid userId, WebSocket socket, CancellationToken cancellationToken = default)
        {
            Added.Add((userId, socket));
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid userId, WebSocket socket, CancellationToken cancellationToken = default)
        {
            Removed.Add((userId, socket));
            return Task.CompletedTask;
        }

        public Task SendToUserAsync(Guid userId, ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken = default)
        {
            Payloads.Add(payload.ToArray());
            return Task.CompletedTask;
        }

        public int GetConnectionCount(Guid userId) =>
            Added.Count(item => item.UserId == userId) - Removed.Count(item => item.UserId == userId);
    }

    private sealed class TestWebSocketFeature(WebSocket socket) : IHttpWebSocketFeature
    {
        public bool IsWebSocketRequest => true;
        public bool WasAccepted { get; private set; }
        public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context)
        {
            WasAccepted = true;
            return Task.FromResult(socket);
        }
    }

    private sealed class RecordingWebSocket(bool closeOnReceive = false) : WebSocket
    {
        private readonly object _gate = new();
        private WebSocketState _state = WebSocketState.Open;
        private WebSocketCloseStatus? _closeStatus;
        private string? _closeStatusDescription;
        public List<byte[]> Messages { get; } = [];
        public override WebSocketCloseStatus? CloseStatus => _closeStatus;
        public override string? CloseStatusDescription => _closeStatusDescription;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;

        public void SetState(WebSocketState state) => _state = state;
        public override void Abort() => _state = WebSocketState.Aborted;
        public override void Dispose() => _state = WebSocketState.Closed;

        public override Task CloseAsync(WebSocketCloseStatus closeStatus,
            string? statusDescription, CancellationToken cancellationToken)
        {
            _closeStatus = closeStatus;
            _closeStatusDescription = statusDescription;
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus,
            string? statusDescription, CancellationToken cancellationToken)
        {
            _closeStatus = closeStatus;
            _closeStatusDescription = statusDescription;
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            if (closeOnReceive)
            {
                _state = WebSocketState.CloseReceived;
                return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close,
                    true, WebSocketCloseStatus.NormalClosure, "done"));
            }

            return Task.FromException<WebSocketReceiveResult>(
                new InvalidOperationException("No receive result configured."));
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType,
            bool endOfMessage, CancellationToken cancellationToken)
        {
            lock (_gate)
                Messages.Add(buffer.ToArray());
            return Task.CompletedTask;
        }

        public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer,
            WebSocketMessageType messageType, bool endOfMessage,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
                Messages.Add(buffer.ToArray());
            return ValueTask.CompletedTask;
        }
    }
}
