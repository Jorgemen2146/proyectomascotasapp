using DogPlatform.Identity.Application.Features.Profile.SocialDirectory;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace DogPlatform.Identity.API.Controllers;
public sealed record SocialUserCardsRequest(IReadOnlyCollection<Guid> UserIds);
[ApiController,Route("api/v1/internal/identity/users")]
public sealed class InternalIdentityController(IMediator mediator,IConfiguration configuration):ControllerBase
{
 bool Authorized()=>Request.Headers.TryGetValue("X-DogPlatform-Internal-Key",out var key)&&!string.IsNullOrWhiteSpace(configuration["InternalServices:ApiKey"])&&key==configuration["InternalServices:ApiKey"];
 [HttpGet("{userId:guid}/social-card")]public async Task<IActionResult> Card(Guid userId,CancellationToken ct){if(!Authorized())return Unauthorized();var value=await mediator.Send(new GetSocialUserCardQuery(userId),ct);return value is null?NotFound():Ok(value);}
 [HttpPost("social-cards")]public async Task<IActionResult> Cards(SocialUserCardsRequest request,CancellationToken ct){if(!Authorized())return Unauthorized();if(request.UserIds.Count>100)return BadRequest();return Ok(await mediator.Send(new GetSocialUserCardsQuery(request.UserIds),ct));}
 [HttpGet("search")]public async Task<IActionResult> Search([FromQuery]string query,[FromQuery]int limit=20,CancellationToken ct=default){if(!Authorized())return Unauthorized();return Ok(await mediator.Send(new SearchSocialUsersQuery(query,limit),ct));}
}
