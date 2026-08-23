using DogPlatform.Health.Application.Features.Vaccinations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Health.API.Controllers;

[ApiController]
[Route("api/v1/health/vaccines")]
public sealed class VaccinesController : HealthApiControllerBase
{
    private readonly IMediator _mediator;
    public VaccinesController(IMediator mediator) => _mediator = mediator;

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int speciesId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVaccinesQuery(speciesId), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }
}
