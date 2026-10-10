using DogPlatform.SharedKernel.Primitives;
using DogPlatform.Social.Application;
using DogPlatform.Social.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace DogPlatform.Social.Tests;

public sealed class SocialFlowTests
{
    [Fact]
    public async Task Complete_social_and_chat_flow_enforces_authorization_blocking_and_read_state()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var current = new MutableCurrentUser(a);
        var store = new InMemorySocialStore();
        var directory = new FakeDirectory(a, b, c);
        var events = new RecordingPublisher();
        var handler = Handler(store, current, directory, events);

        var sent = await handler.Handle(new SendFriendRequestCommand(b), default);
        Assert.True(sent.IsSuccess);
        Assert.Equal(a, sent.Value.RequesterUserId);
        Assert.Equal(b, sent.Value.AddresseeUserId);
        Assert.Contains(events.Events, e => e.UserId == b && e.Name == "social.friend_request.received");

        var searchFromA = await handler.Handle(new SearchUsersQuery("user", 20), default);
        Assert.Equal(SocialStatuses.PendingSent,
            searchFromA.Value.Single(user => user.UserId == b).SocialStatus);

        current.UserId = b;
        var incoming = await handler.Handle(new ListRequestsQuery(true, 1, 20), default);
        Assert.Single(incoming.Value.Items);
        Assert.Equal(1, incoming.Value.TotalItems);
        var searchFromB = await handler.Handle(new SearchUsersQuery("user", 20), default);
        Assert.Equal(SocialStatuses.PendingReceived,
            searchFromB.Value.Single(user => user.UserId == a).SocialStatus);

        current.UserId = c;
        var unauthorizedAccept = await handler.Handle(
            new RespondFriendRequestCommand(sent.Value.Id, true), default);
        Assert.True(unauthorizedAccept.IsFailure);
        Assert.Equal(SocialErrors.Unauthorized, unauthorizedAccept.Error);

        current.UserId = b;
        var accepted = await handler.Handle(
            new RespondFriendRequestCommand(sent.Value.Id, true), default);
        Assert.True(accepted.IsSuccess);
        Assert.Contains(events.Events, e => e.UserId == a && e.Name == "social.friend_request.accepted");

        foreach (var userId in new[] { a, b })
        {
            current.UserId = userId;
            var friends = await handler.Handle(new ListFriendsQuery(1, 20), default);
            Assert.Single(friends.Value.Items);
            Assert.Equal(1, friends.Value.TotalItems);
            Assert.Equal(1, friends.Value.TotalPages);
            Assert.False(friends.Value.HasNextPage);
        }

        current.UserId = a;
        var conversation = await handler.Handle(new CreateConversationCommand(b), default);
        Assert.True(conversation.IsSuccess);
        var message = await handler.Handle(
            new SendMessageCommand(conversation.Value.Id, "hola"), default);
        Assert.True(message.IsSuccess);
        Assert.Equal(a, message.Value.SenderUserId);
        Assert.Single(store.MessagesData);
        Assert.Contains(events.Events, e => e.UserId == b && e.Name == "chat.message.received");

        current.UserId = c;
        var foreignRead = await handler.Handle(
            new GetConversationQuery(conversation.Value.Id), default);
        Assert.True(foreignRead.IsFailure);
        Assert.Equal(SocialErrors.Unauthorized, foreignRead.Error);
        var foreignMarkRead = await handler.Handle(
            new MarkConversationReadCommand(conversation.Value.Id), default);
        Assert.True(foreignMarkRead.IsFailure);
        Assert.Equal(SocialErrors.Unauthorized, foreignMarkRead.Error);

        current.UserId = b;
        var conversations = await handler.Handle(new ListConversationsQuery(1, 20), default);
        Assert.Single(conversations.Value.Items);
        Assert.Equal(1, conversations.Value.Items.Single().UnreadCount);
        Assert.Equal(1, conversations.Value.TotalItems);
        var marked = await handler.Handle(
            new MarkConversationReadCommand(conversation.Value.Id), default);
        Assert.True(marked.IsSuccess);
        Assert.Contains(events.Events, e => e.UserId == a && e.Name == "chat.message.read");
        conversations = await handler.Handle(new ListConversationsQuery(1, 20), default);
        Assert.Equal(0, conversations.Value.Items.Single().UnreadCount);

        var blocked = await handler.Handle(new BlockUserCommand(a), default);
        Assert.True(blocked.IsSuccess);
        Assert.False(await store.AreFriends(a, b, default));
        var hidden = await handler.Handle(new ListConversationsQuery(1, 20), default);
        Assert.Empty(hidden.Value.Items);
        Assert.Equal(0, hidden.Value.TotalItems);
        Assert.Equal(0, hidden.Value.TotalPages);
        Assert.False(hidden.Value.HasNextPage);
        var noFriends = await handler.Handle(new ListFriendsQuery(1, 20), default);
        Assert.Empty(noFriends.Value.Items);
        Assert.Equal(0, noFriends.Value.TotalItems);
        var blocks = await handler.Handle(new ListBlocksQuery(1, 20), default);
        Assert.Single(blocks.Value.Items);
        Assert.Equal(1, blocks.Value.TotalItems);

        current.UserId = a;
        Assert.True((await handler.Handle(
            new SendMessageCommand(conversation.Value.Id, "blocked"), default)).IsFailure);
        Assert.True((await handler.Handle(new CreateConversationCommand(b), default)).IsFailure);
        current.UserId = b;
        Assert.True((await handler.Handle(new CreateConversationCommand(a), default)).IsFailure);

        Assert.True((await handler.Handle(new UnblockUserCommand(a), default)).IsSuccess);
        Assert.False(await store.AreFriends(a, b, default));
        Assert.True((await handler.Handle(new CreateConversationCommand(a), default)).IsFailure);
    }

    private static SocialHandlers Handler(InMemorySocialStore store,
        MutableCurrentUser current, IIdentityDirectory directory, RecordingPublisher events) =>
        new(store, current, directory, events, events, TimeProvider.System,
            NullLogger<SocialHandlers>.Instance);

    private sealed class MutableCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; set; } = userId;
    }

    private sealed class FakeDirectory(params Guid[] ids) : IIdentityDirectory
    {
        private readonly UserCard[] _users = ids.Select(id =>
            new UserCard(id, $"user-{id:N}", null)).ToArray();
        public Task<UserCard?> GetAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(_users.SingleOrDefault(user => user.UserId == userId));
        public Task<IReadOnlyCollection<UserCard>> GetManyAsync(IEnumerable<Guid> userIds,
            CancellationToken ct = default)
        {
            var wanted = userIds.ToHashSet();
            return Task.FromResult<IReadOnlyCollection<UserCard>>(
                _users.Where(user => wanted.Contains(user.UserId)).ToArray());
        }
        public Task<IReadOnlyCollection<UserCard>> SearchAsync(string query, int limit,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyCollection<UserCard>>(_users.Take(limit).ToArray());
    }

    private sealed class RecordingPublisher : IRealtimePublisher, ISocialNotificationPublisher
    {
        public List<(Guid UserId, string Name, object Data)> Events { get; } = [];
        public Task PublishAsync(Guid userId, string eventName, object data,
            CancellationToken ct = default)
        {
            Events.Add((userId, eventName, data));
            return Task.CompletedTask;
        }
        public Task PublishAsync(Guid userId, string eventName, string title, string message,
            string referenceType, string referenceId, object data, string deduplicationKey,
            CancellationToken ct = default)
        {
            Events.Add((userId, eventName, data));
            return Task.CompletedTask;
        }
    }

    private sealed class InMemorySocialStore : ISocialStore
    {
        private readonly List<FriendRequest> _requests = [];
        private readonly List<Friendship> _friendships = [];
        private readonly List<UserBlock> _blocks = [];
        private readonly List<Conversation> _conversations = [];
        public List<Message> MessagesData { get; } = [];

        public Task<bool> AreFriends(Guid a, Guid b, CancellationToken ct)
        {
            var pair = Friendship.Order(a, b);
            return Task.FromResult(_friendships.Any(x => x.User1Id == pair.Item1 && x.User2Id == pair.Item2));
        }
        public Task<bool> IsBlockedEitherWay(Guid a, Guid b, CancellationToken ct) =>
            Task.FromResult(_blocks.Any(x =>
                x.BlockerUserId == a && x.BlockedUserId == b ||
                x.BlockerUserId == b && x.BlockedUserId == a));
        public Task<FriendRequest?> GetRequest(Guid id, CancellationToken ct) =>
            Task.FromResult(_requests.SingleOrDefault(x => x.Id == id));
        public Task<bool> PendingRequestExists(Guid a, Guid b, CancellationToken ct) =>
            Task.FromResult(_requests.Any(x => x.Status == FriendRequestStatus.Pending &&
                (x.RequesterUserId == a && x.AddresseeUserId == b ||
                 x.RequesterUserId == b && x.AddresseeUserId == a)));
        public void Add(FriendRequest value) => _requests.Add(value);
        public void Add(Friendship value) => _friendships.Add(value);
        public void Add(UserBlock value) => _blocks.Add(value);
        public void Add(Conversation value) => _conversations.Add(value);
        public void Add(Message value) => MessagesData.Add(value);
        public Task<Friendship?> GetFriendship(Guid a, Guid b, CancellationToken ct)
        {
            var pair = Friendship.Order(a, b);
            return Task.FromResult(_friendships.SingleOrDefault(x =>
                x.User1Id == pair.Item1 && x.User2Id == pair.Item2));
        }
        public void Remove(Friendship value) => _friendships.Remove(value);
        public Task<UserBlock?> GetBlock(Guid blocker, Guid blocked, CancellationToken ct) =>
            Task.FromResult(_blocks.SingleOrDefault(x =>
                x.BlockerUserId == blocker && x.BlockedUserId == blocked));
        public void Remove(UserBlock value) => _blocks.Remove(value);
        public Task<IReadOnlyCollection<FriendRequest>> GetPendingBetween(Guid a, Guid b,
            CancellationToken ct) => Task.FromResult<IReadOnlyCollection<FriendRequest>>(
            _requests.Where(x => x.Status == FriendRequestStatus.Pending &&
                (x.RequesterUserId == a && x.AddresseeUserId == b ||
                 x.RequesterUserId == b && x.AddresseeUserId == a)).ToArray());
        public Task<Conversation?> GetConversation(Guid id, CancellationToken ct) =>
            Task.FromResult(_conversations.SingleOrDefault(x => x.Id == id));
        public Task<Conversation?> GetConversationBetween(Guid a, Guid b, CancellationToken ct)
        {
            var pair = Friendship.Order(a, b);
            return Task.FromResult(_conversations.SingleOrDefault(x =>
                x.User1Id == pair.Item1 && x.User2Id == pair.Item2));
        }
        public async Task<(IReadOnlyCollection<Friendship>, int)> Friends(Guid user, int page,
            int size, CancellationToken ct)
        {
            var visible = new List<Friendship>();
            foreach (var item in _friendships.Where(x => x.User1Id == user || x.User2Id == user))
                if (!await IsBlockedEitherWay(user, item.User1Id == user ? item.User2Id : item.User1Id, ct))
                    visible.Add(item);
            return (Page(visible, page, size), visible.Count);
        }
        public async Task<(IReadOnlyCollection<FriendRequest>, int)> Requests(Guid user,
            bool incoming, int page, int size, CancellationToken ct)
        {
            var visible = new List<FriendRequest>();
            foreach (var item in _requests.Where(x => x.Status == FriendRequestStatus.Pending)
                         .Where(x => incoming ? x.AddresseeUserId == user : x.RequesterUserId == user))
                if (!await IsBlockedEitherWay(item.RequesterUserId, item.AddresseeUserId, ct))
                    visible.Add(item);
            return (Page(visible, page, size), visible.Count);
        }
        public Task<(IReadOnlyCollection<UserBlock>, int)> Blocks(Guid user, int page,
            int size, CancellationToken ct)
        {
            var items = _blocks.Where(x => x.BlockerUserId == user).ToArray();
            return Task.FromResult<(IReadOnlyCollection<UserBlock>, int)>((Page(items, page, size), items.Length));
        }
        public async Task<(IReadOnlyCollection<Conversation>, int)> Conversations(Guid user,
            int page, int size, CancellationToken ct)
        {
            var visible = new List<Conversation>();
            foreach (var item in _conversations.Where(x => x.Includes(user)))
                if (!await IsBlockedEitherWay(user, item.Other(user), ct)) visible.Add(item);
            return (Page(visible, page, size), visible.Count);
        }
        public Task<(IReadOnlyCollection<Message>, bool)> Messages(Guid conversation,
            DateTime? before, Guid? beforeId, int size, CancellationToken ct)
        {
            var query = MessagesData.Where(x => x.ConversationId == conversation && x.DeletedAtUtc is null)
                .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).ToArray();
            return Task.FromResult<(IReadOnlyCollection<Message>, bool)>((query.Take(size).ToArray(), query.Length > size));
        }
        public Task<Message?> LatestMessage(Guid conversation, CancellationToken ct) =>
            Task.FromResult(MessagesData.Where(x => x.ConversationId == conversation && x.DeletedAtUtc is null)
                .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).FirstOrDefault());
        public async Task<int> UnreadCount(Guid conversation, Guid user, CancellationToken ct)
        {
            var entity = await GetConversation(conversation, ct);
            var participant = entity!.Participants.Single(x => x.UserId == user);
            return MessagesData.Count(x => x.ConversationId == conversation &&
                x.SenderUserId != user && x.DeletedAtUtc is null &&
                (!participant.LastReadAtUtc.HasValue || x.CreatedAtUtc > participant.LastReadAtUtc));
        }
        public Task Save(CancellationToken ct) => Task.CompletedTask;
        public Task ExecuteSerializable(Func<CancellationToken, Task<Result>> action,
            CancellationToken ct) => action(ct);
        private static IReadOnlyCollection<T> Page<T>(IEnumerable<T> items, int page, int size) =>
            items.Skip((page - 1) * size).Take(size).ToArray();
    }
}