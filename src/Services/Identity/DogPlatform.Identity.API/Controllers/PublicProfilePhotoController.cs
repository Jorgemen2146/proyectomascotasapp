using DogPlatform.Identity.Application.Features.Profile.Photo;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DogPlatform.Identity.API.Controllers;
[ApiController,Authorize,Route("api/v1/auth/users")]
public sealed class PublicProfilePhotoController(IMediator mediator):ControllerBase
{
 [HttpGet("{userId:guid}/photo/content")]
 public async Task<IActionResult> Get(Guid userId,CancellationToken ct)
 {
  var result=await mediator.Send(new GetProfilePhotoContentQuery(userId),ct);
  if(result.IsSuccess)return File(result.Value.Stream,result.Value.ContentType);
  return result.Error.Type==ErrorType.NotFound?NotFound(result.Error):StatusCode(500,result.Error);
 }
}
