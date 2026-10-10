using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Social.Domain;

public enum FriendRequestStatus { Pending, Accepted, Rejected, Cancelled }

public static class SocialErrors
{
    public static readonly Error SelfRequest = Error.Validation("Social.SelfRequest", "You cannot send a friend request to yourself.");
    public static readonly Error AlreadyFriends = Error.Conflict("Social.AlreadyFriends", "The users are already friends.");
    public static readonly Error DuplicateRequest = Error.Conflict("Social.DuplicateRequest", "A pending friend request already exists between these users.");
    public static readonly Error Blocked = Error.Conflict("Social.Blocked", "This operation is unavailable while either user has blocked the other.");
    public static readonly Error NotFound = Error.NotFound("Social.NotFound", "The requested social resource was not found.");
    public static readonly Error Unauthorized = Error.Unauthorized("Social.Unauthorized", "The authenticated user cannot perform this operation.");
    public static readonly Error NotFriends = Error.Conflict("Social.NotFriends", "Only friends can start conversations or send messages.");
    public static readonly Error InvalidMessage = Error.Validation("Social.InvalidMessage", "Message content must contain 1 to 2000 characters.");
    public static readonly Error InvalidPagination = Error.Validation("Social.InvalidPagination", "Page must be positive and pageSize must be between 1 and 100.");
}

public sealed class FriendRequest : AggregateRoot<Guid>
{
    private FriendRequest() { }
    private FriendRequest(Guid id, Guid requester, Guid addressee, DateTime now) : base(id)
    { RequesterUserId = requester; AddresseeUserId = addressee; Status = FriendRequestStatus.Pending; CreatedAtUtc = now; }
    public Guid RequesterUserId { get; private set; }
    public Guid AddresseeUserId { get; private set; }
    public FriendRequestStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RespondedAtUtc { get; private set; }
    public static Result<FriendRequest> Create(Guid requester, Guid addressee, DateTime now) =>
        requester == addressee ? Result.Failure<FriendRequest>(SocialErrors.SelfRequest) : Result.Success(new FriendRequest(Guid.NewGuid(), requester, addressee, now));
    public Result Accept(Guid actor, DateTime now) => Complete(actor, now, FriendRequestStatus.Accepted);
    public Result Reject(Guid actor, DateTime now) => Complete(actor, now, FriendRequestStatus.Rejected);
    public Result Cancel(Guid actor, DateTime now)
    {
        if (actor != RequesterUserId) return Result.Failure(SocialErrors.Unauthorized);
        if (Status != FriendRequestStatus.Pending) return Result.Failure(SocialErrors.DuplicateRequest);
        Status = FriendRequestStatus.Cancelled; RespondedAtUtc = now; return Result.Success();
    }
    public void CancelDueToBlock(DateTime now) { if (Status == FriendRequestStatus.Pending) { Status = FriendRequestStatus.Cancelled; RespondedAtUtc = now; } }
    private Result Complete(Guid actor, DateTime now, FriendRequestStatus status)
    {
        if (actor != AddresseeUserId) return Result.Failure(SocialErrors.Unauthorized);
        if (Status != FriendRequestStatus.Pending) return Result.Failure(SocialErrors.DuplicateRequest);
        Status = status; RespondedAtUtc = now; return Result.Success();
    }
}

public sealed class Friendship : Entity<Guid>
{
    private Friendship() { }
    private Friendship(Guid id, Guid user1, Guid user2, DateTime now) : base(id) { User1Id = user1; User2Id = user2; CreatedAtUtc = now; }
    public Guid User1Id { get; private set; }
    public Guid User2Id { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public static Friendship Create(Guid a, Guid b, DateTime now) { var (u1,u2)=Order(a,b); return new(Guid.NewGuid(),u1,u2,now); }
    public static (Guid,Guid) Order(Guid a, Guid b) => a.CompareTo(b) < 0 ? (a,b) : (b,a);
}

public sealed class UserBlock : Entity<Guid>
{
    private UserBlock() { }
    private UserBlock(Guid blocker, Guid blocked, DateTime now) : base(Guid.NewGuid()) { BlockerUserId=blocker; BlockedUserId=blocked; CreatedAtUtc=now; }
    public Guid BlockerUserId { get; private set; }
    public Guid BlockedUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public static Result<UserBlock> Create(Guid blocker, Guid blocked, DateTime now) => blocker == blocked ? Result.Failure<UserBlock>(SocialErrors.SelfRequest) : Result.Success(new UserBlock(blocker, blocked, now));
}

public sealed class Conversation : AggregateRoot<Guid>
{
    private readonly List<ConversationParticipant> _participants=[];
    private Conversation() { }
    private Conversation(Guid a, Guid b, DateTime now) : base(Guid.NewGuid()) { var (u1,u2)=Friendship.Order(a,b); User1Id=u1;User2Id=u2;CreatedAtUtc=now; _participants.Add(new(Id,a,now));_participants.Add(new(Id,b,now)); }
    public Guid User1Id { get; private set; }
    public Guid User2Id { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastMessageAtUtc { get; private set; }
    public IReadOnlyCollection<ConversationParticipant> Participants => _participants;
    public static Conversation Create(Guid a, Guid b, DateTime now) => new(a,b,now);
    public bool Includes(Guid userId) => User1Id==userId || User2Id==userId;
    public Guid Other(Guid userId) => User1Id==userId ? User2Id : User1Id;
    public void RecordMessage(DateTime now) => LastMessageAtUtc=now;
}

public sealed class ConversationParticipant
{
    private ConversationParticipant() { }
    internal ConversationParticipant(Guid conversationId, Guid userId, DateTime now) { ConversationId=conversationId;UserId=userId;JoinedAtUtc=now; }
    public Guid ConversationId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }
    public Guid? LastReadMessageId { get; private set; }
    public DateTime? LastReadAtUtc { get; private set; }
    public void MarkRead(Guid? messageId, DateTime now) { LastReadMessageId=messageId;LastReadAtUtc=now; }
}

public sealed class Message : Entity<Guid>
{
    private Message() { }
    private Message(Guid conversationId, Guid sender, string content, DateTime now) : base(Guid.NewGuid()) { ConversationId=conversationId;SenderUserId=sender;Content=content;CreatedAtUtc=now; }
    public Guid ConversationId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Content { get; private set; }=string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? EditedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public static Result<Message> Create(Guid conversationId, Guid sender, string? content, DateTime now) =>
        string.IsNullOrWhiteSpace(content) || content.Trim().Length > 2000 ? Result.Failure<Message>(SocialErrors.InvalidMessage) : Result.Success(new Message(conversationId,sender,content.Trim(),now));
}
