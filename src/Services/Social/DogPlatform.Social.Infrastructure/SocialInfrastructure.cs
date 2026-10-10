using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DogPlatform.SharedKernel.Primitives;
using DogPlatform.Social.Application;
using DogPlatform.Social.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DogPlatform.Social.Infrastructure;

public sealed class SocialDbContext(DbContextOptions<SocialDbContext> options):DbContext(options)
{
 public DbSet<FriendRequest> FriendRequests=>Set<FriendRequest>(); public DbSet<Friendship> Friendships=>Set<Friendship>(); public DbSet<UserBlock>UserBlocks=>Set<UserBlock>(); public DbSet<Conversation>Conversations=>Set<Conversation>(); public DbSet<ConversationParticipant>ConversationParticipants=>Set<ConversationParticipant>(); public DbSet<Message>Messages=>Set<Message>();
 protected override void OnModelCreating(ModelBuilder m)
 {
  m.HasDefaultSchema("social");
  var r=m.Entity<FriendRequest>();r.ToTable("FriendRequests",t=>t.HasCheckConstraint("CK_FriendRequests_DifferentUsers","[RequesterUserId] <> [AddresseeUserId]"));r.HasKey(x=>x.Id);r.Property(x=>x.Id).ValueGeneratedNever();r.Property(x=>x.Status).HasConversion<string>().HasMaxLength(20);r.HasIndex(x=>new{x.RequesterUserId,x.AddresseeUserId,x.Status});
  var f=m.Entity<Friendship>();f.ToTable("Friendships");f.HasKey(x=>x.Id);f.Property(x=>x.Id).ValueGeneratedNever();f.HasIndex(x=>new{x.User1Id,x.User2Id}).IsUnique();
  var b=m.Entity<UserBlock>();b.ToTable("UserBlocks",t=>t.HasCheckConstraint("CK_UserBlocks_DifferentUsers","[BlockerUserId] <> [BlockedUserId]"));b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedNever();b.HasIndex(x=>new{x.BlockerUserId,x.BlockedUserId}).IsUnique();
  var c=m.Entity<Conversation>();c.ToTable("Conversations");c.HasKey(x=>x.Id);c.Property(x=>x.Id).ValueGeneratedNever();c.HasIndex(x=>new{x.User1Id,x.User2Id}).IsUnique();c.HasMany(x=>x.Participants).WithOne().HasForeignKey(x=>x.ConversationId).OnDelete(DeleteBehavior.Cascade);c.Navigation(x=>x.Participants).UsePropertyAccessMode(PropertyAccessMode.Field);
  var p=m.Entity<ConversationParticipant>();p.ToTable("ConversationParticipants");p.HasKey(x=>new{x.ConversationId,x.UserId});p.HasIndex(x=>x.UserId);
  var msg=m.Entity<Message>();msg.ToTable("Messages");msg.HasKey(x=>x.Id);msg.Property(x=>x.Id).ValueGeneratedNever();msg.Property(x=>x.Content).HasMaxLength(2000).IsRequired();msg.HasIndex(x=>new{x.ConversationId,x.CreatedAtUtc,x.Id}).IsDescending(false,true,true);msg.HasOne<Conversation>().WithMany().HasForeignKey(x=>x.ConversationId).OnDelete(DeleteBehavior.Cascade);
 }
}

public sealed class SocialStore(SocialDbContext db):ISocialStore
{
 public Task<bool> AreFriends(Guid a,Guid b,CancellationToken ct){var(u1,u2)=Friendship.Order(a,b);return db.Friendships.AnyAsync(x=>x.User1Id==u1&&x.User2Id==u2,ct);}
 public Task<bool> IsBlockedEitherWay(Guid a,Guid b,CancellationToken ct)=>db.UserBlocks.AnyAsync(x=>(x.BlockerUserId==a&&x.BlockedUserId==b)||(x.BlockerUserId==b&&x.BlockedUserId==a),ct);
 public Task<FriendRequest?> GetRequest(Guid id,CancellationToken ct)=>db.FriendRequests.FirstOrDefaultAsync(x=>x.Id==id,ct);
 public Task<bool> PendingRequestExists(Guid a,Guid b,CancellationToken ct)=>db.FriendRequests.AnyAsync(x=>x.Status==FriendRequestStatus.Pending&&((x.RequesterUserId==a&&x.AddresseeUserId==b)||(x.RequesterUserId==b&&x.AddresseeUserId==a)),ct);
 public void Add(FriendRequest v)=>db.Add(v);public void Add(Friendship v)=>db.Add(v);public void Add(UserBlock v)=>db.Add(v);public void Add(Conversation v)=>db.Add(v);public void Add(Message v)=>db.Add(v);
 public async Task<Friendship?> GetFriendship(Guid a,Guid b,CancellationToken ct){var(u1,u2)=Friendship.Order(a,b);return await db.Friendships.FirstOrDefaultAsync(x=>x.User1Id==u1&&x.User2Id==u2,ct);}public void Remove(Friendship v)=>db.Remove(v);
 public Task<UserBlock?> GetBlock(Guid a,Guid b,CancellationToken ct)=>db.UserBlocks.FirstOrDefaultAsync(x=>x.BlockerUserId==a&&x.BlockedUserId==b,ct);public void Remove(UserBlock v)=>db.Remove(v);
 public async Task<IReadOnlyCollection<FriendRequest>> GetPendingBetween(Guid a,Guid b,CancellationToken ct)=>await db.FriendRequests.Where(x=>x.Status==FriendRequestStatus.Pending&&((x.RequesterUserId==a&&x.AddresseeUserId==b)||(x.RequesterUserId==b&&x.AddresseeUserId==a))).ToListAsync(ct);
 public Task<Conversation?> GetConversation(Guid id,CancellationToken ct)=>db.Conversations.Include(x=>x.Participants).FirstOrDefaultAsync(x=>x.Id==id,ct);
 public async Task<Conversation?> GetConversationBetween(Guid a,Guid b,CancellationToken ct){var(u1,u2)=Friendship.Order(a,b);return await db.Conversations.Include(x=>x.Participants).FirstOrDefaultAsync(x=>x.User1Id==u1&&x.User2Id==u2,ct);}
 public async Task<(IReadOnlyCollection<Friendship>,int)> Friends(Guid u,int p,int s,CancellationToken ct){var q=db.Friendships.AsNoTracking().Where(x=>x.User1Id==u||x.User2Id==u).Where(x=>!db.UserBlocks.Any(b=>(b.BlockerUserId==u&&b.BlockedUserId==(x.User1Id==u?x.User2Id:x.User1Id))||(b.BlockedUserId==u&&b.BlockerUserId==(x.User1Id==u?x.User2Id:x.User1Id))));return(await q.OrderByDescending(x=>x.CreatedAtUtc).Skip((p-1)*s).Take(s).ToListAsync(ct),await q.CountAsync(ct));}
 public async Task<(IReadOnlyCollection<FriendRequest>,int)> Requests(Guid u,bool incoming,int p,int s,CancellationToken ct){var q=db.FriendRequests.AsNoTracking().Where(x=>x.Status==FriendRequestStatus.Pending).Where(x=>incoming?x.AddresseeUserId==u:x.RequesterUserId==u).Where(x=>!db.UserBlocks.Any(b=>(b.BlockerUserId==x.RequesterUserId&&b.BlockedUserId==x.AddresseeUserId)||(b.BlockerUserId==x.AddresseeUserId&&b.BlockedUserId==x.RequesterUserId)));return(await q.OrderByDescending(x=>x.CreatedAtUtc).Skip((p-1)*s).Take(s).ToListAsync(ct),await q.CountAsync(ct));}
 public async Task<(IReadOnlyCollection<UserBlock>,int)> Blocks(Guid u,int p,int s,CancellationToken ct){var q=db.UserBlocks.AsNoTracking().Where(x=>x.BlockerUserId==u);return(await q.OrderByDescending(x=>x.CreatedAtUtc).Skip((p-1)*s).Take(s).ToListAsync(ct),await q.CountAsync(ct));}
 public async Task<(IReadOnlyCollection<Conversation>,int)> Conversations(Guid u,int p,int s,CancellationToken ct){var q=db.Conversations.AsNoTracking().Where(x=>x.User1Id==u||x.User2Id==u).Where(x=>!db.UserBlocks.Any(b=>(b.BlockerUserId==u&&b.BlockedUserId==(x.User1Id==u?x.User2Id:x.User1Id))||(b.BlockedUserId==u&&b.BlockerUserId==(x.User1Id==u?x.User2Id:x.User1Id))));return(await q.OrderByDescending(x=>x.LastMessageAtUtc??x.CreatedAtUtc).Skip((p-1)*s).Take(s).ToListAsync(ct),await q.CountAsync(ct));}
 public async Task<(IReadOnlyCollection<Message>,bool)> Messages(Guid c,DateTime? before,Guid? beforeId,int s,CancellationToken ct){var q=db.Messages.AsNoTracking().Where(x=>x.ConversationId==c&&x.DeletedAtUtc==null);if(before.HasValue)q=q.Where(x=>x.CreatedAtUtc<before||(x.CreatedAtUtc==before&&beforeId.HasValue&&x.Id.CompareTo(beforeId.Value)<0));var list=await q.OrderByDescending(x=>x.CreatedAtUtc).ThenByDescending(x=>x.Id).Take(s+1).ToListAsync(ct);return(list.Take(s).ToArray(),list.Count>s);}
 public Task<Message?> LatestMessage(Guid c,CancellationToken ct)=>db.Messages.AsNoTracking().Where(x=>x.ConversationId==c&&x.DeletedAtUtc==null).OrderByDescending(x=>x.CreatedAtUtc).ThenByDescending(x=>x.Id).FirstOrDefaultAsync(ct);
 public async Task<int> UnreadCount(Guid c,Guid u,CancellationToken ct){var p=await db.ConversationParticipants.AsNoTracking().FirstAsync(x=>x.ConversationId==c&&x.UserId==u,ct);return await db.Messages.CountAsync(x=>x.ConversationId==c&&x.SenderUserId!=u&&x.DeletedAtUtc==null&&(!p.LastReadAtUtc.HasValue||x.CreatedAtUtc>p.LastReadAtUtc),ct);}
 public Task Save(CancellationToken ct)=>db.SaveChangesAsync(ct);
 public async Task ExecuteSerializable(Func<CancellationToken,Task<Result>> action,CancellationToken ct){await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);var r=await action(ct);if(r.IsSuccess)await tx.CommitAsync(ct);else await tx.RollbackAsync(ct);}
}

public sealed class CurrentUser(IHttpContextAccessor accessor):ICurrentUser { public Guid UserId { get { var user=accessor.HttpContext?.User;var value=user?.FindFirst(ClaimTypes.NameIdentifier)?.Value??user?.FindFirst("sub")?.Value;return Guid.TryParse(value,out var id)?id:throw new UnauthorizedAccessException(); } } }
public sealed class IdentityDirectory(HttpClient http):IIdentityDirectory
{
 public async Task<UserCard?> GetAsync(Guid id,CancellationToken ct){var response=await http.GetAsync($"api/v1/internal/identity/users/{id}/social-card",ct);if(response.StatusCode==System.Net.HttpStatusCode.NotFound)return null;response.EnsureSuccessStatusCode();return await response.Content.ReadFromJsonAsync<UserCard>(cancellationToken:ct);}
 public async Task<IReadOnlyCollection<UserCard>> GetManyAsync(IEnumerable<Guid> ids,CancellationToken ct){var distinct=ids.Where(id=>id!=Guid.Empty).Distinct().ToArray();if(distinct.Length==0)return Array.Empty<UserCard>();var response=await http.PostAsJsonAsync("api/v1/internal/identity/users/social-cards",new{userIds=distinct},ct);response.EnsureSuccessStatusCode();return await response.Content.ReadFromJsonAsync<UserCard[]>(cancellationToken:ct)??[];}
 public async Task<IReadOnlyCollection<UserCard>> SearchAsync(string q,int limit,CancellationToken ct)=>await http.GetFromJsonAsync<UserCard[]>($"api/v1/internal/identity/users/search?query={Uri.EscapeDataString(q)}&limit={limit}",ct)??[];
}
public sealed class NotificationPublisher(HttpClient http,ILogger<NotificationPublisher> log):IRealtimePublisher,ISocialNotificationPublisher
{
 public async Task PublishAsync(Guid user,string evt,object data,CancellationToken ct=default)=>await SendAsync(user,evt,evt,evt,"Social",string.Empty,data,$"realtime:{evt}:{Guid.NewGuid()}",false,ct);
 public async Task PublishAsync(Guid user,string evt,string title,string message,string referenceType,string referenceId,object data,string key,CancellationToken ct=default)=>await SendAsync(user,evt,title,message,referenceType,referenceId,data,key,true,ct);
 private async Task SendAsync(Guid user,string evt,string title,string message,string referenceType,string referenceId,object data,string key,bool persist,CancellationToken ct){try{var response=await http.PostAsJsonAsync("api/v1/internal/notifications",new{userId=user,eventName=evt,title,message,referenceType,referenceId,data,deduplicationKey=key,persist},ct);if(!response.IsSuccessStatusCode)log.LogWarning("Notifications returned {Status} for {Event}",response.StatusCode,evt);}catch(Exception ex)when(ex is not OperationCanceledException){log.LogWarning(ex,"Best-effort notification failed for {Event}",evt);}}
}
public static class DependencyInjection
{
 public static IServiceCollection AddInfrastructure(this IServiceCollection s,IConfiguration c){s.AddDbContext<SocialDbContext>(o=>o.UseSqlServer(c.GetConnectionString("SocialDb")??throw new InvalidOperationException("ConnectionStrings:SocialDb is required.")));s.AddScoped<ISocialStore,SocialStore>();s.AddScoped<ICurrentUser,CurrentUser>();var key=c["InternalServices:ApiKey"]??throw new InvalidOperationException("InternalServices:ApiKey is required.");static void Client(HttpClient x,string url,string key){x.BaseAddress=new Uri(url);x.Timeout=TimeSpan.FromSeconds(10);x.DefaultRequestHeaders.Add("X-DogPlatform-Internal-Key",key);}s.AddHttpClient<IIdentityDirectory,IdentityDirectory>(x=>Client(x,c["IdentityService:BaseUrl"]??"http://localhost:5102",key));s.AddHttpClient<NotificationPublisher>(x=>Client(x,c["NotificationsService:BaseUrl"]??"http://localhost:5109",key));s.AddScoped<IRealtimePublisher>(x=>x.GetRequiredService<NotificationPublisher>());s.AddScoped<ISocialNotificationPublisher>(x=>x.GetRequiredService<NotificationPublisher>());return s;}
}
