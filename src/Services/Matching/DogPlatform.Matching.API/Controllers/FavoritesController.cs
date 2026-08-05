using DogPlatform.Matching.Application.Features.AddFavorite;
using DogPlatform.Matching.Application.Features.GetFavorites;
using DogPlatform.Matching.Application.Features.RemoveFavorite;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Matching.API.Controllers;

/// <summary>
/// Manages a pet's favorite candidates.
/// </summary>
[ApiController]
[Route("api/v1/matching/pets/{petId:guid}/favorites")]
[Authorize]
public sealed class FavoritesController : MatchingApiControllerBase
{
    private readonly IMediator _mediator;

    public FavoritesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lists the favorite candidates for a pet.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFavorites(
        Guid petId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetFavoritesQuery(petId, pageNumber, pageSize), cancellationToken);

        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    /// <summary>Adds a candidate to a pet's favorites.</summary>
    [HttpPut("{candidatePetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddFavorite(
        Guid petId, Guid candidatePetId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddFavoriteCommand(petId, candidatePetId), cancellationToken);

        return result.IsFailure ? FromError(result.Error) : NoContent();
    }

    /// <summary>Removes a candidate from a pet's favorites.</summary>
    [HttpDelete("{candidatePetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFavorite(
        Guid petId, Guid candidatePetId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RemoveFavoriteCommand(petId, candidatePetId), cancellationToken);

        return result.IsFailure ? FromError(result.Error) : NoContent();
    }
}
