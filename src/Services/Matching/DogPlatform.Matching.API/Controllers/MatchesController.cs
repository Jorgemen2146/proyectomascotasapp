using DogPlatform.Matching.API.Requests;
using DogPlatform.Matching.Application.Features.Matches;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Matching.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/matching")]
public sealed class MatchesController(IMediator mediator) : MatchingApiControllerBase
{
    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetMatchesQuery(), cancellationToken));

    [HttpGet("matches/{matchId:guid}")]
    public async Task<IActionResult> GetMatch(Guid matchId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMatchDetailQuery(matchId), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    [HttpPost("matches/{matchId:guid}/breeding-intents")]
    public async Task<IActionResult> ProposeIntent(Guid matchId,
        [FromBody] ProposeBreedingIntentRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ProposeBreedingIntentCommand(
            matchId, request.Notes, request.ExpectedDateUtc), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : StatusCode(201, result.Value);
    }

    [HttpGet("matches/{matchId:guid}/breeding-intent")]
    public async Task<IActionResult> GetIntent(Guid matchId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBreedingIntentQuery(matchId), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    [HttpPost("breeding-intents/{intentId:guid}/accept")]
    public async Task<IActionResult> AcceptIntent(Guid intentId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AcceptBreedingIntentCommand(intentId), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    [HttpPost("breeding-intents/{intentId:guid}/cancel")]
    public async Task<IActionResult> CancelIntent(Guid intentId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelBreedingIntentCommand(intentId), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }
}
