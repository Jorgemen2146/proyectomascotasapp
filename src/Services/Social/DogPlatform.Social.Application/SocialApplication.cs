using DogPlatform.SharedKernel.Primitives;
using DogPlatform.Social.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Social.Application;

public sealed record Page<T>(IReadOnlyCollection<T> Items,int PageNumber,int PageSize,int TotalItems)
{ public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalItems/PageSize); public bool HasNextPage=>PageNumber<TotalPages; }
public sealed record UserCard(Guid UserId,string DisplayName,string? ProfilePhotoUrl,string? SocialStatus=null);
public sealed record FriendRequestDto(Guid Id,Guid RequesterUserId,Guid AddresseeUserId,string Status,DateTime CreatedAtUtc,DateTime? RespondedAtUtc,UserCard? User=null);
public sealed record FriendshipDto(Guid FriendshipId,UserCard User,DateTime CreatedAtUtc);
public sealed record BlockDto(Guid BlockId,UserCard User,DateTime CreatedAtUtc);
public sealed record MessageDto(Guid Id,Guid ConversationId,Guid SenderUserId,string Content,DateTime CreatedAtUtc,DateTime? EditedAtUtc,DateTime? DeletedAtUtc);
public sealed record ConversationDto(Guid Id,UserCard Counterpart,MessageDto? LastMessage,DateTime CreatedAtUtc,DateTime? LastMessageAtUtc,int UnreadCount);

public interface ICurrentUser { Guid UserId { get; } }
public interface IRealtimePublisher { Task PublishAsync(Guid userId,string eventName,object data,CancellationToken ct=default); }
public interface ISocialNotificationPublisher { Task PublishAsync(Guid userId,string eventName,string title,string message,string referenceType,string referenceId,object data,string deduplicationKey,CancellationToken ct=default); }
public interface IIdentityDirectory
{
    Task<UserCard?> GetAsync(Guid userId,CancellationToken ct=default);
    Task<IReadOnlyCollection<UserCard>> GetManyAsync(IEnumerable<Guid> userIds,CancellationToken ct=default);
    Task<IReadOnlyCollection<UserCard>> SearchAsync(string query,int limit,CancellationToken ct=default);
}
public interface ISocialStore
{
    Task<bool> AreFriends(Guid a,Guid b,CancellationToken ct); Task<bool> IsBlockedEitherWay(Guid a,Guid b,CancellationToken ct);
    Task<FriendRequest?> GetRequest(Guid id,CancellationToken ct); Task<bool> PendingRequestExists(Guid a,Guid b,CancellationToken ct);
    void Add(FriendRequest value); void Add(Friendship value); void Add(UserBlock value); void Add(Conversation value); void Add(Message value);
    Task<Friendship?> GetFriendship(Guid a,Guid b,CancellationToken ct); void Remove(Friendship value);
    Task<UserBlock?> GetBlock(Guid blocker,Guid blocked,CancellationToken ct); void Remove(UserBlock value);
    Task<IReadOnlyCollection<FriendRequest>> GetPendingBetween(Guid a,Guid b,CancellationToken ct);
    Task<Conversation?> GetConversation(Guid id,CancellationToken ct); Task<Conversation?> GetConversationBetween(Guid a,Guid b,CancellationToken ct);
    Task<(IReadOnlyCollection<Friendship>,int)> Friends(Guid user,int page,int size,CancellationToken ct);
    Task<(IReadOnlyCollection<FriendRequest>,int)> Requests(Guid user,bool incoming,int page,int size,CancellationToken ct);
    Task<(IReadOnlyCollection<UserBlock>,int)> Blocks(Guid user,int page,int size,CancellationToken ct);
    Task<(IReadOnlyCollection<Conversation>,int)> Conversations(Guid user,int page,int size,CancellationToken ct);
    Task<(IReadOnlyCollection<Message>,bool)> Messages(Guid conversation,DateTime? before,Guid? beforeId,int size,CancellationToken ct);
    Task<Message?> LatestMessage(Guid conversation,CancellationToken ct); Task<int> UnreadCount(Guid conversation,Guid user,CancellationToken ct);
    Task Save(CancellationToken ct); Task ExecuteSerializable(Func<CancellationToken,Task<Result>> action,CancellationToken ct);
}

public sealed record SendFriendRequestCommand(Guid AddresseeUserId):IRequest<Result<FriendRequestDto>>;
public sealed record RespondFriendRequestCommand(Guid RequestId,bool Accept):IRequest<Result<FriendRequestDto>>;
public sealed record CancelFriendRequestCommand(Guid RequestId):IRequest<Result>;
public sealed record BlockUserCommand(Guid UserId):IRequest<Result>;
public sealed record UnblockUserCommand(Guid UserId):IRequest<Result>;
public sealed record CreateConversationCommand(Guid UserId):IRequest<Result<ConversationDto>>;
public sealed record SendMessageCommand(Guid ConversationId,string Content):IRequest<Result<MessageDto>>;
public sealed record MarkConversationReadCommand(Guid ConversationId):IRequest<Result>;
public sealed record ListFriendsQuery(int Page,int PageSize):IRequest<Result<Page<FriendshipDto>>>;
public sealed record ListRequestsQuery(bool Incoming,int Page,int PageSize):IRequest<Result<Page<FriendRequestDto>>>;
public sealed record ListBlocksQuery(int Page,int PageSize):IRequest<Result<Page<BlockDto>>>;
public sealed record ListConversationsQuery(int Page,int PageSize):IRequest<Result<Page<ConversationDto>>>;
public sealed record GetConversationQuery(Guid Id):IRequest<Result<ConversationDto>>;
public sealed record ListMessagesQuery(Guid ConversationId,DateTime? Before,Guid? BeforeId,int PageSize):IRequest<Result<object>>;
public sealed record SearchUsersQuery(string Query,int Limit):IRequest<Result<IReadOnlyCollection<UserCard>>>;

public sealed class SocialHandlers(ISocialStore store,ICurrentUser current,IIdentityDirectory directory,IRealtimePublisher realtime,ISocialNotificationPublisher notifications,TimeProvider clock) :
    IRequestHandler<SendFriendRequestCommand,Result<FriendRequestDto>>, IRequestHandler<RespondFriendRequestCommand,Result<FriendRequestDto>>, IRequestHandler<CancelFriendRequestCommand,Result>,
    IRequestHandler<BlockUserCommand,Result>, IRequestHandler<UnblockUserCommand,Result>, IRequestHandler<CreateConversationCommand,Result<ConversationDto>>,
    IRequestHandler<SendMessageCommand,Result<MessageDto>>, IRequestHandler<MarkConversationReadCommand,Result>, IRequestHandler<ListFriendsQuery,Result<Page<FriendshipDto>>>,
    IRequestHandler<ListRequestsQuery,Result<Page<FriendRequestDto>>>, IRequestHandler<ListBlocksQuery,Result<Page<BlockDto>>>, IRequestHandler<ListConversationsQuery,Result<Page<ConversationDto>>>,
    IRequestHandler<GetConversationQuery,Result<ConversationDto>>, IRequestHandler<ListMessagesQuery,Result<object>>, IRequestHandler<SearchUsersQuery,Result<IReadOnlyCollection<UserCard>>>
{
    DateTime Now=>clock.GetUtcNow().UtcDateTime;
    static bool Invalid(int p,int s)=>p<1||s is <1 or >100;
    static FriendRequestDto Map(FriendRequest r,UserCard? u=null)=>new(r.Id,r.RequesterUserId,r.AddresseeUserId,r.Status.ToString(),r.CreatedAtUtc,r.RespondedAtUtc,u);
    static MessageDto Map(Message m)=>new(m.Id,m.ConversationId,m.SenderUserId,m.Content,m.CreatedAtUtc,m.EditedAtUtc,m.DeletedAtUtc);
    public async Task<Result<FriendRequestDto>> Handle(SendFriendRequestCommand q,CancellationToken ct)
    {
        var made=FriendRequest.Create(current.UserId,q.AddresseeUserId,Now); if(made.IsFailure)return Result.Failure<FriendRequestDto>(made.Error);
        if(await store.IsBlockedEitherWay(current.UserId,q.AddresseeUserId,ct))return Result.Failure<FriendRequestDto>(SocialErrors.Blocked);
        if(await store.AreFriends(current.UserId,q.AddresseeUserId,ct))return Result.Failure<FriendRequestDto>(SocialErrors.AlreadyFriends);
        if(await store.PendingRequestExists(current.UserId,q.AddresseeUserId,ct))return Result.Failure<FriendRequestDto>(SocialErrors.DuplicateRequest);
        if(await directory.GetAsync(q.AddresseeUserId,ct) is null)return Result.Failure<FriendRequestDto>(SocialErrors.NotFound);
        store.Add(made.Value); await store.Save(ct); var dto=Map(made.Value);
        await BestEffort(q.AddresseeUserId,"social.friend_request.received",dto,"Solicitud de amistad","Recibiste una solicitud de amistad.","FriendRequest",made.Value.Id.ToString(),$"friend-request:{made.Value.Id}:received",ct); return Result.Success(dto);
    }
    public async Task<Result<FriendRequestDto>> Handle(RespondFriendRequestCommand q,CancellationToken ct)
    {
        var r=await store.GetRequest(q.RequestId,ct);if(r is null)return Result.Failure<FriendRequestDto>(SocialErrors.NotFound);
        var result=q.Accept?r.Accept(current.UserId,Now):r.Reject(current.UserId,Now);if(result.IsFailure)return Result.Failure<FriendRequestDto>(result.Error);
        if(q.Accept){if(await store.IsBlockedEitherWay(r.RequesterUserId,r.AddresseeUserId,ct))return Result.Failure<FriendRequestDto>(SocialErrors.Blocked);if(!await store.AreFriends(r.RequesterUserId,r.AddresseeUserId,ct))store.Add(Friendship.Create(r.RequesterUserId,r.AddresseeUserId,Now));}
        await store.Save(ct);var dto=Map(r);if(q.Accept)await BestEffort(r.RequesterUserId,"social.friend_request.accepted",dto,"Solicitud aceptada","Tu solicitud de amistad fue aceptada.","FriendRequest",r.Id.ToString(),$"friend-request:{r.Id}:accepted",ct);return Result.Success(dto);
    }
    public async Task<Result> Handle(CancelFriendRequestCommand q,CancellationToken ct){var r=await store.GetRequest(q.RequestId,ct);if(r is null)return Result.Failure(SocialErrors.NotFound);var x=r.Cancel(current.UserId,Now);if(x.IsFailure)return x;await store.Save(ct);return Result.Success();}
    public async Task<Result> Handle(BlockUserCommand q,CancellationToken ct)
    {
        var b=UserBlock.Create(current.UserId,q.UserId,Now);if(b.IsFailure)return Result.Failure(b.Error);if(await store.GetBlock(current.UserId,q.UserId,ct) is not null)return Result.Success();
        await store.ExecuteSerializable(async token=>{store.Add(b.Value);var f=await store.GetFriendship(current.UserId,q.UserId,token);if(f is not null)store.Remove(f);foreach(var r in await store.GetPendingBetween(current.UserId,q.UserId,token))r.CancelDueToBlock(Now);await store.Save(token);return Result.Success();},ct);return Result.Success();
    }
    public async Task<Result> Handle(UnblockUserCommand q,CancellationToken ct){var b=await store.GetBlock(current.UserId,q.UserId,ct);if(b is null)return Result.Success();store.Remove(b);await store.Save(ct);return Result.Success();}
    public async Task<Result<ConversationDto>> Handle(CreateConversationCommand q,CancellationToken ct)
    { if(!await store.AreFriends(current.UserId,q.UserId,ct))return Result.Failure<ConversationDto>(SocialErrors.NotFriends);if(await store.IsBlockedEitherWay(current.UserId,q.UserId,ct))return Result.Failure<ConversationDto>(SocialErrors.Blocked);var c=await store.GetConversationBetween(current.UserId,q.UserId,ct);if(c is null){c=Conversation.Create(current.UserId,q.UserId,Now);store.Add(c);await store.Save(ct);}return Result.Success(await ConversationMap(c,ct)); }
    public async Task<Result<MessageDto>> Handle(SendMessageCommand q,CancellationToken ct)
    {var c=await store.GetConversation(q.ConversationId,ct);if(c is null)return Result.Failure<MessageDto>(SocialErrors.NotFound);if(!c.Includes(current.UserId))return Result.Failure<MessageDto>(SocialErrors.Unauthorized);var other=c.Other(current.UserId);if(!await store.AreFriends(current.UserId,other,ct))return Result.Failure<MessageDto>(SocialErrors.NotFriends);if(await store.IsBlockedEitherWay(current.UserId,other,ct))return Result.Failure<MessageDto>(SocialErrors.Blocked);var made=Message.Create(c.Id,current.UserId,q.Content,Now);if(made.IsFailure)return Result.Failure<MessageDto>(made.Error);store.Add(made.Value);c.RecordMessage(made.Value.CreatedAtUtc);await store.Save(ct);var dto=Map(made.Value);await BestEffort(other,"chat.message.received",dto,"Nuevo mensaje","Tienes un nuevo mensaje.","Conversation",c.Id.ToString(),$"chat-message:{made.Value.Id}",ct);return Result.Success(dto);}
    public async Task<Result> Handle(MarkConversationReadCommand q,CancellationToken ct){var c=await store.GetConversation(q.ConversationId,ct);if(c is null)return Result.Failure(SocialErrors.NotFound);var p=c.Participants.SingleOrDefault(x=>x.UserId==current.UserId);if(p is null)return Result.Failure(SocialErrors.Unauthorized);var last=await store.LatestMessage(c.Id,ct);p.MarkRead(last?.Id,Now);await store.Save(ct);if(last is not null)try{await realtime.PublishAsync(c.Other(current.UserId),"chat.message.read",new{conversationId=c.Id,readerUserId=current.UserId,lastReadMessageId=last.Id,readAtUtc=Now},ct);}catch{}return Result.Success();}
    public async Task<Result<Page<FriendshipDto>>> Handle(ListFriendsQuery q,CancellationToken ct){if(Invalid(q.Page,q.PageSize))return Result.Failure<Page<FriendshipDto>>(SocialErrors.InvalidPagination);var (items,total)=await store.Friends(current.UserId,q.Page,q.PageSize,ct);var ids=items.Select(x=>x.User1Id==current.UserId?x.User2Id:x.User1Id);var users=(await directory.GetManyAsync(ids,ct)).ToDictionary(x=>x.UserId);return Result.Success(new Page<FriendshipDto>(items.Where(x=>users.ContainsKey(x.User1Id==current.UserId?x.User2Id:x.User1Id)).Select(x=>new FriendshipDto(x.Id,users[x.User1Id==current.UserId?x.User2Id:x.User1Id],x.CreatedAtUtc)).ToArray(),q.Page,q.PageSize,total));}
    public async Task<Result<Page<FriendRequestDto>>> Handle(ListRequestsQuery q,CancellationToken ct){if(Invalid(q.Page,q.PageSize))return Result.Failure<Page<FriendRequestDto>>(SocialErrors.InvalidPagination);var(i,t)=await store.Requests(current.UserId,q.Incoming,q.Page,q.PageSize,ct);var ids=i.Select(x=>q.Incoming?x.RequesterUserId:x.AddresseeUserId);var u=(await directory.GetManyAsync(ids,ct)).ToDictionary(x=>x.UserId);return Result.Success(new Page<FriendRequestDto>(i.Select(x=>Map(x,u.GetValueOrDefault(q.Incoming?x.RequesterUserId:x.AddresseeUserId))).ToArray(),q.Page,q.PageSize,t));}
    public async Task<Result<Page<BlockDto>>> Handle(ListBlocksQuery q,CancellationToken ct){if(Invalid(q.Page,q.PageSize))return Result.Failure<Page<BlockDto>>(SocialErrors.InvalidPagination);var(i,t)=await store.Blocks(current.UserId,q.Page,q.PageSize,ct);var u=(await directory.GetManyAsync(i.Select(x=>x.BlockedUserId),ct)).ToDictionary(x=>x.UserId);return Result.Success(new Page<BlockDto>(i.Where(x=>u.ContainsKey(x.BlockedUserId)).Select(x=>new BlockDto(x.Id,u[x.BlockedUserId],x.CreatedAtUtc)).ToArray(),q.Page,q.PageSize,t));}
    public async Task<Result<Page<ConversationDto>>> Handle(ListConversationsQuery q,CancellationToken ct){if(Invalid(q.Page,q.PageSize))return Result.Failure<Page<ConversationDto>>(SocialErrors.InvalidPagination);var(i,t)=await store.Conversations(current.UserId,q.Page,q.PageSize,ct);var mapped=new List<ConversationDto>();foreach(var c in i)if(!await store.IsBlockedEitherWay(current.UserId,c.Other(current.UserId),ct))mapped.Add(await ConversationMap(c,ct));return Result.Success(new Page<ConversationDto>(mapped,q.Page,q.PageSize,t));}
    public async Task<Result<ConversationDto>> Handle(GetConversationQuery q,CancellationToken ct){var c=await store.GetConversation(q.Id,ct);return c is null?Result.Failure<ConversationDto>(SocialErrors.NotFound):!c.Includes(current.UserId)?Result.Failure<ConversationDto>(SocialErrors.Unauthorized):await store.IsBlockedEitherWay(current.UserId,c.Other(current.UserId),ct)?Result.Failure<ConversationDto>(SocialErrors.NotFound):Result.Success(await ConversationMap(c,ct));}
    public async Task<Result<object>> Handle(ListMessagesQuery q,CancellationToken ct){if(q.PageSize is <1 or >100)return Result.Failure<object>(SocialErrors.InvalidPagination);var c=await store.GetConversation(q.ConversationId,ct);if(c is null)return Result.Failure<object>(SocialErrors.NotFound);if(!c.Includes(current.UserId))return Result.Failure<object>(SocialErrors.Unauthorized);var(i,more)=await store.Messages(c.Id,q.Before,q.BeforeId,q.PageSize,ct);return Result.Success<object>(new{items=i.Select(Map).ToArray(),nextCursor=i.Count==0?null:new{before=i.Last().CreatedAtUtc,beforeId=i.Last().Id},hasMore=more});}
    public async Task<Result<IReadOnlyCollection<UserCard>>> Handle(SearchUsersQuery q,CancellationToken ct){if(string.IsNullOrWhiteSpace(q.Query)||q.Limit is <1 or >50)return Result.Failure<IReadOnlyCollection<UserCard>>(SocialErrors.InvalidPagination);var found=await directory.SearchAsync(q.Query,q.Limit,ct);var usable=new List<UserCard>();foreach(var x in found)if(x.UserId!=current.UserId&&!await store.IsBlockedEitherWay(current.UserId,x.UserId,ct))usable.Add(x with{SocialStatus=await store.AreFriends(current.UserId,x.UserId,ct)?"Friends":await store.PendingRequestExists(current.UserId,x.UserId,ct)?"Pending":"None"});return Result.Success<IReadOnlyCollection<UserCard>>(usable);}
    async Task<ConversationDto> ConversationMap(Conversation c,CancellationToken ct){var user=await directory.GetAsync(c.Other(current.UserId),ct)??new UserCard(c.Other(current.UserId),"Usuario",null);var last=await store.LatestMessage(c.Id,ct);return new(c.Id,user,last is null?null:Map(last),c.CreatedAtUtc,c.LastMessageAtUtc,await store.UnreadCount(c.Id,current.UserId,ct));}
    async Task BestEffort(Guid user,string evt,object data,string title,string message,string rt,string rid,string key,CancellationToken ct){try{await notifications.PublishAsync(user,evt,title,message,rt,rid,data,key,ct);}catch{try{await realtime.PublishAsync(user,evt,data,ct);}catch{}}}
}

public static class DependencyInjection { public static IServiceCollection AddApplication(this IServiceCollection s){s.AddMediatR(x=>x.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));s.AddSingleton(TimeProvider.System);return s;} }
