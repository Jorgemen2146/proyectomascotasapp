using DogPlatform.Matching.API.Requests;
using DogPlatform.Matching.Application.Features.AcceptMatchRequest;
using DogPlatform.Matching.Application.Features.CancelMatchRequest;
using DogPlatform.Matching.Application.Features.CreateMatchRequest;
using DogPlatform.Matching.Application.Features.GetIncomingRequests;
using DogPlatform.Matching.Application.Features.GetOutgoingRequests;
using DogPlatform.Matching.Application.Features.RejectMatchRequest;
using DogPlatform.Matching.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Matching.API.Controllers;

/// <summary>
/// Manages match/cruce requests between pets. OwnerId is always derived
/// from the JWT via ICurrentUser inside the handlers.
/// </summary>
[ApiController]
[Route("api/v1/matching/requests")]
[Authorize]
public sealed class MatchRequestsController : MatchingApiControllerBase
{
    private readonly IMediator _mediator;

    public MatchRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Creates a new match/cruce request.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMatchRequestRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateMatchRequestCommand(
            request.PetId, request.CandidatePetId, request.Message, request.SharePhoneNumber);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return FromError(result.Error);

        return CreatedAtAction(
            nameof(GetIncoming), new { }, result.Value);
    }

    /// <summary>Lists incoming match requests for the authenticated owner's pets.</summary>
    [HttpGet("incoming")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetIncoming(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] MatchRequestStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetIncomingRequestsQuery(pageNumber, pageSize, status), cancellationToken);

        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    /// <summary>Lists outgoing match requests created by the authenticated owner's pets.</summary>
    [HttpGet("outgoing")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOutgoing(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] MatchRequestStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetOutgoingRequestsQuery(pageNumber, pageSize, status), cancellationToken);

        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    /// <summary>Accepts an incoming match request. Only the candidate pet's owner can accept.</summary>
    [HttpPost("{matchRequestId:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(Guid matchRequestId,
        [FromBody] AcceptMatchRequestRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AcceptMatchRequestCommand(matchRequestId, request?.SharePhoneNumber ?? false), cancellationToken);

        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    /// <summary>Rejects an incoming match request. Only the candidate pet's owner can reject.</summary>
    [HttpPost("{matchRequestId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(Guid matchRequestId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RejectMatchRequestCommand(matchRequestId), cancellationToken);

        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    /// <summary>Cancels an outgoing match request. Only the requester pet's owner can cancel.</summary>
    [HttpPost("{matchRequestId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid matchRequestId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CancelMatchRequestCommand(matchRequestId), cancellationToken);

        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }
}
