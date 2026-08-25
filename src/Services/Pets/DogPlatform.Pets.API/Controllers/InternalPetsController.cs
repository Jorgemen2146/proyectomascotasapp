using DogPlatform.Authentication;
using DogPlatform.Pets.Application.Features.Pets.GetVaccinationContexts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Pets.API.Controllers;

[ApiController]
[Route("api/v1/internal/pets")]
[Authorize(AuthenticationSchemes = InternalServiceDefaults.AuthenticationScheme)]
public sealed class InternalPetsController(IMediator mediator) : ControllerBase
{
    [HttpGet("vaccination-context")]
    public async Task<IActionResult> GetVaccinationContexts(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetVaccinationContextsQuery(), cancellationToken));
}
