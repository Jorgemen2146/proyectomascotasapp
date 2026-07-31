using DogPlatform.Pets.Application.Features.Pets.Create;
using DogPlatform.Pets.Application.Features.Pets.Delete;
using DogPlatform.Pets.Application.Features.Pets.GetById;
using DogPlatform.Pets.Application.Features.Pets.GetMine;
using DogPlatform.Pets.Application.Features.Pets.Update;
using DogPlatform.Pets.API.Requests.Pets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Pets.API.Controllers;

[ApiController]
[Route("api/v1/pets")]
[Authorize]
public sealed class PetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new pet for the authenticated user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePet(
        [FromBody] CreatePetRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePetCommand(
            request.BreedId,
            request.Name,
            request.BirthDate,
            request.Gender,
            request.Weight,
            request.Color,
            request.PedigreeNumber,
            request.IsSterilized,
            request.Description);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetPetById), new { id = result.Value.PetId }, result.Value);
    }

    /// <summary>
    /// Get all pets for the authenticated user, with optional pagination, filtering and sorting.
    /// OwnerId is taken exclusively from the JWT — do not pass it as a parameter.
    /// </summary>
    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPets(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? name = null,
        [FromQuery] int? speciesId = null,
        [FromQuery] int? breedId = null,
        [FromQuery] string? sex = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "DESC",
        CancellationToken cancellationToken = default)
    {
        var query = new GetMyPetsQuery(
            pageNumber, pageSize, name, speciesId, breedId, sex, sortBy, sortDirection);

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a specific pet by ID (if owned by the authenticated user).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetPetByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Pet.NotFound" => NotFound(),
                "Pet.Unauthorized" => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update a pet (if owned by the authenticated user).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePet(
        [FromRoute] Guid id,
        [FromBody] UpdatePetRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePetCommand(
            id,
            request.Name,
            request.BirthDate,
            request.Gender,
            request.Weight,
            request.Color,
            request.PedigreeNumber,
            request.IsSterilized,
            request.Description);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Pet.NotFound" => NotFound(),
                "Pet.Unauthorized" => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Soft-delete a pet (if owned by the authenticated user).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePet(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeletePetCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Pet.NotFound" or "Pet.AlreadyDeleted" => NotFound(),
                "Pet.Unauthorized" => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return NoContent();
    }
}
