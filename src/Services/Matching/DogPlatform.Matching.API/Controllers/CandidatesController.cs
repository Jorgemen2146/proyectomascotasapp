using DogPlatform.Matching.Application.Features.GetCandidateDetail;
using DogPlatform.Matching.Application.Features.SearchCandidates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Matching.API.Controllers;

/// <summary>
/// Candidate search and detail. Never exposes OwnerId, contact info, or
/// medical history of candidates.
/// </summary>
[ApiController]
[Route("api/v1/matching/pets/{petId:guid}/candidates")]
[Authorize]
public sealed class CandidatesController : MatchingApiControllerBase
{
    private readonly IMediator _mediator;

    public CandidatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Searches compatible candidates for a given pet.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search(
        Guid petId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? breedId = null,
        [FromQuery] int? minimumAgeMonths = null,
        [FromQuery] int? maximumAgeMonths = null,
        [FromQuery] PedigreeFilter pedigree = PedigreeFilter.Any,
        [FromQuery] int? minimumScore = null,
        [FromQuery] string sortBy = "CompatibilityScore",
        [FromQuery] string sortDirection = "DESC",
        [FromQuery] bool favoritesOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchCandidatesQuery(
            petId,
            pageNumber,
            pageSize,
            breedId,
            minimumAgeMonths,
            maximumAgeMonths,
            minimumScore,
            sortBy,
            sortDirection,
            favoritesOnly,
            pedigree);

        var result = await _mediator.Send(query, cancellationToken);

        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    /// <summary>Gets the detailed compatibility breakdown for a single candidate.</summary>
    [HttpGet("{candidatePetId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(
        Guid petId, Guid candidatePetId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCandidateDetailQuery(petId, candidatePetId), cancellationToken);

        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }
}
