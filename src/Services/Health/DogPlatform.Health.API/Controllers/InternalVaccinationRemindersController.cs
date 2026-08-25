using DogPlatform.Authentication;
using DogPlatform.Health.Application.Features.Vaccinations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Health.API.Controllers;

[ApiController]
[Route("api/v1/health/internal/vaccination-reminders")]
[Authorize(AuthenticationSchemes = InternalServiceDefaults.AuthenticationScheme)]
public sealed class InternalVaccinationRemindersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetVaccinationReminderCandidatesQuery(date), cancellationToken);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }
}
