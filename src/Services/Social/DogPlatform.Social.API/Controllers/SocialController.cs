using DogPlatform.SharedKernel.Primitives;
using DogPlatform.Social.Application;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DogPlatform.Social.API.Controllers;

public sealed record SendFriendRequestBody(Guid AddresseeUserId); public sealed record CreateConversationBody(Guid UserId); public sealed record SendMessageBody(string Content);
[ApiController,Authorize,Route("api/v1/social")]
public sealed class SocialController(IMediator mediator):ControllerBase
{
 IActionResult Error(Error e)=>e.Type switch{ErrorType.NotFound=>NotFound(e),ErrorType.Conflict=>Conflict(e),ErrorType.Unauthorized=>StatusCode(403,e),ErrorType.Validation=>BadRequest(e),_=>BadRequest(e)};
 async Task<IActionResult> Value<T>(IRequest<Result<T>> q,CancellationToken ct,int? success=null){var r=await mediator.Send(q,ct);return r.IsFailure?Error(r.Error):StatusCode(success??200,r.Value);}
 async Task<IActionResult> Execute(IRequest<Result> q,CancellationToken ct){var r=await mediator.Send(q,ct);return r.IsFailure?Error(r.Error):NoContent();}
 [HttpGet("users/search")]public Task<IActionResult> Search([FromQuery]string query,[FromQuery]int limit=20,CancellationToken ct=default)=>Value(new SearchUsersQuery(query,limit),ct);
 [HttpGet("friends")]public Task<IActionResult> Friends([FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default)=>Value(new ListFriendsQuery(page,pageSize),ct);
 [HttpGet("friend-requests/incoming")]public Task<IActionResult> Incoming([FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default)=>Value(new ListRequestsQuery(true,page,pageSize),ct);
 [HttpGet("friend-requests/outgoing")]public Task<IActionResult> Outgoing([FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default)=>Value(new ListRequestsQuery(false,page,pageSize),ct);
 [HttpPost("friend-requests")]public Task<IActionResult> SendFriendRequest(SendFriendRequestBody body,CancellationToken ct)=>Value(new SendFriendRequestCommand(body.AddresseeUserId),ct,201);
 [HttpPost("friend-requests/{id:guid}/accept")]public Task<IActionResult> Accept(Guid id,CancellationToken ct)=>Value(new RespondFriendRequestCommand(id,true),ct);
 [HttpPost("friend-requests/{id:guid}/reject")]public Task<IActionResult> Reject(Guid id,CancellationToken ct)=>Value(new RespondFriendRequestCommand(id,false),ct);
 [HttpDelete("friend-requests/{id:guid}")]public Task<IActionResult> Cancel(Guid id,CancellationToken ct)=>Execute(new CancelFriendRequestCommand(id),ct);
 [HttpGet("blocked-users")]public Task<IActionResult> Blocks([FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default)=>Value(new ListBlocksQuery(page,pageSize),ct);
 [HttpPost("users/{userId:guid}/block")]public Task<IActionResult> Block(Guid userId,CancellationToken ct)=>Execute(new BlockUserCommand(userId),ct);
 [HttpDelete("users/{userId:guid}/block")]public Task<IActionResult> Unblock(Guid userId,CancellationToken ct)=>Execute(new UnblockUserCommand(userId),ct);
 [HttpGet("conversations")]public Task<IActionResult> Conversations([FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default)=>Value(new ListConversationsQuery(page,pageSize),ct);
 [HttpPost("conversations")]public Task<IActionResult> CreateConversation(CreateConversationBody body,CancellationToken ct)=>Value(new CreateConversationCommand(body.UserId),ct,201);
 [HttpGet("conversations/{id:guid}")]public Task<IActionResult> Conversation(Guid id,CancellationToken ct)=>Value(new GetConversationQuery(id),ct);
 [HttpGet("conversations/{id:guid}/messages")]public Task<IActionResult> Messages(Guid id,[FromQuery]DateTime? before,[FromQuery]Guid? beforeId,[FromQuery]int pageSize=50,CancellationToken ct=default)=>Value(new ListMessagesQuery(id,before,beforeId,pageSize),ct);
 [HttpPost("conversations/{id:guid}/messages")]public Task<IActionResult> Send(Guid id,SendMessageBody body,CancellationToken ct)=>Value(new SendMessageCommand(id,body.Content),ct,201);
 [HttpPost("conversations/{id:guid}/read")]public Task<IActionResult> Read(Guid id,CancellationToken ct)=>Execute(new MarkConversationReadCommand(id),ct);
}
