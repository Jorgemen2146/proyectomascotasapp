using DogPlatform.Matching.Application.Features.GetCandidateDetail;
using DogPlatform.Matching.Application.Features.SearchCandidates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Matching.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/matching")]
public sealed class MatchingSearchController(IMediator mediator) : MatchingApiControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] Guid petId,
        [FromQuery] int? breedId = null,
        [FromQuery] int? minAgeMonths = null,
        [FromQuery] int? maxAgeMonths = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new SearchCandidatesQuery(
            petId, pageNumber, pageSize, breedId, minAgeMonths, maxAgeMonths,
            null, "CompatibilityScore", "DESC", false), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    [HttpGet("pets/{candidatePetId:guid}")]
    public async Task<IActionResult> PublicDetail(Guid candidatePetId,
        [FromQuery] Guid sourcePetId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetCandidateDetailQuery(sourcePetId, candidatePetId), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }
}
