using DogPlatform.Pets.Application.Features.Breeds.GetBySpecies;
using DogPlatform.Pets.Application.Features.Species.GetAll;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Pets.API.Controllers;

[ApiController]
[Route("api/v1/species")]
[AllowAnonymous]
public sealed class SpeciesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpeciesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all available species.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSpecies(CancellationToken cancellationToken)
    {
        var query = new GetAllSpeciesQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get all breeds for a given species.
    /// </summary>
    [HttpGet("{speciesId:int}/breeds")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBreedsBySpecies(
        [FromRoute] int speciesId,
        CancellationToken cancellationToken)
    {
        if (speciesId <= 0)
            return BadRequest("speciesId must be greater than 0.");

        var query = new GetBreedsBySpeciesQuery(speciesId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
            return NotFound();

        return Ok(result.Value);
    }
}
