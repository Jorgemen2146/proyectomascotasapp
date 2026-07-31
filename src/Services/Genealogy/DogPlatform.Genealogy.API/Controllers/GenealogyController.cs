using DogPlatform.Genealogy.Application.Features.AssignParents;
using DogPlatform.Genealogy.Application.Features.GetParents;
using DogPlatform.Genealogy.Application.Features.RemoveFather;
using DogPlatform.Genealogy.Application.Features.RemoveMother;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Genealogy.API.Controllers;

[ApiController]
[Route("api/v1/genealogy")]
[Authorize]
public sealed class GenealogyController : ControllerBase
{
    private readonly IMediator _mediator;

    public GenealogyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Assigns or replaces the father and/or mother of a pet.
    /// Ownership is verified from the JWT — do NOT pass ownerId.
    /// </summary>
    [HttpPut("{petId:guid}/parents")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignParents(
        Guid petId,
        [FromBody] AssignParentsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignParentsCommand(petId, request.FatherId, request.MotherId);
        var result  = await _mediator.Send(command, cancellationToken);

        return result.IsFailure
            ? MapError(result.Error)
            : NoContent();
    }

    /// <summary>
    /// Gets the direct parents (father and/or mother) of a pet.
    /// </summary>
    [HttpGet("{petId:guid}/parents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParents(
        Guid petId,
        CancellationToken cancellationToken)
    {
        var query  = new GetParentsQuery(petId);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsFailure
            ? MapError(result.Error)
            : Ok(result.Value);
    }

    /// <summary>
    /// Removes the father relationship for the specified pet.
    /// </summary>
    [HttpDelete("{petId:guid}/father")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFather(
        Guid petId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveFatherCommand(petId);
        var result  = await _mediator.Send(command, cancellationToken);

        return result.IsFailure
            ? MapError(result.Error)
            : NoContent();
    }

    /// <summary>
    /// Removes the mother relationship for the specified pet.
    /// </summary>
    [HttpDelete("{petId:guid}/mother")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMother(
        Guid petId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveMotherCommand(petId);
        var result  = await _mediator.Send(command, cancellationToken);

        return result.IsFailure
            ? MapError(result.Error)
            : NoContent();
    }

    // ── Error mapping ──────────────────────────────────────────────────────

    private IActionResult MapError(Error error) => error.Type switch
    {
        ErrorType.NotFound     => NotFound(error),
        ErrorType.Unauthorized => Forbid(),
        ErrorType.Validation   => BadRequest(error),
        _                      => BadRequest(error)
    };
}

// ── Request DTO ────────────────────────────────────────────────────────────

public sealed record AssignParentsRequest(Guid? FatherId, Guid? MotherId);
