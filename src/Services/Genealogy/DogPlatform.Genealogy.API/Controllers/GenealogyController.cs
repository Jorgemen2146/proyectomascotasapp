using DogPlatform.Genealogy.Application.Features.AssignParents;
using DogPlatform.Genealogy.Application.Features.GetAncestors;
using DogPlatform.Genealogy.Application.Features.GetAncestorTree;
using DogPlatform.Genealogy.Application.Features.GetDescendants;
using DogPlatform.Genealogy.Application.Features.GetParents;
using DogPlatform.Genealogy.Application.Features.GetSiblings;
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

    /// <summary>
    /// Returns the ancestor tree (father/mother, grandparents, great-grandparents, ...) of a pet.
    /// </summary>
    [HttpGet("{petId:guid}/tree")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAncestorTree(
        Guid petId,
        [FromQuery] int? depth,
        CancellationToken cancellationToken)
    {
        var query  = new GetAncestorTreeQuery(petId, depth);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsFailure
            ? MapError(result.Error)
            : Ok(result.Value);
    }

    /// <summary>
    /// Returns a flattened list of ancestors with the lineage path(s) leading to each one.
    /// </summary>
    [HttpGet("{petId:guid}/ancestors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAncestors(
        Guid petId,
        [FromQuery] int? depth,
        CancellationToken cancellationToken)
    {
        var query  = new GetAncestorsQuery(petId, depth);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsFailure
            ? MapError(result.Error)
            : Ok(result.Value);
    }

    /// <summary>
    /// Returns the descendants of a pet (children, grandchildren, great-grandchildren, ...).
    /// </summary>
    [HttpGet("{petId:guid}/descendants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDescendants(
        Guid petId,
        [FromQuery] int? depth,
        CancellationToken cancellationToken)
    {
        var query  = new GetDescendantsQuery(petId, depth);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsFailure
            ? MapError(result.Error)
            : Ok(result.Value);
    }

    /// <summary>
    /// Returns the calculated siblings of a pet (full and half siblings), derived from
    /// shared father/mother. There is no siblings table.
    /// </summary>
    [HttpGet("{petId:guid}/siblings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSiblings(
        Guid petId,
        CancellationToken cancellationToken)
    {
        var query  = new GetSiblingsQuery(petId);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsFailure
            ? MapError(result.Error)
            : Ok(result.Value);
    }

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
