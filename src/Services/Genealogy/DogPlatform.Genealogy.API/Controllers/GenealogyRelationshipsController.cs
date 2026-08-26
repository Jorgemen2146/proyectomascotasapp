using DogPlatform.Genealogy.Application.Features.Relationships;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Genealogy.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/genealogy")]
public sealed class GenealogyRelationshipsController(IMediator mediator) : ControllerBase
{
    [HttpPost("pets/{childPetId:guid}/parents")]
    public async Task<IActionResult> AddOwnParent(Guid childPetId,
        AddOwnParentRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddOwnParentCommand(
            childPetId, request.ParentPetId, request.ParentRole), cancellationToken);
        return result.IsFailure ? MapError(result.Error) : StatusCode(201, result.Value);
    }

    [HttpDelete("relationships/{relationshipId:guid}")]
    public async Task<IActionResult> DeleteRelationship(Guid relationshipId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteRelationshipCommand(relationshipId), cancellationToken);
        return result.IsFailure ? MapError(result.Error) : NoContent();
    }

    [HttpPost("invitations")]
    public async Task<IActionResult> CreateInvitation(CreateInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateInvitationCommand(
            request.ChildPetId, request.ParentRole, request.OwnerEmail), cancellationToken);
        return result.IsFailure ? MapError(result.Error) : StatusCode(201, result.Value);
    }

    [HttpGet("invitations/{token}")]
    public async Task<IActionResult> GetInvitation(string token, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInvitationQuery(token), cancellationToken);
        return result.IsFailure ? MapError(result.Error) : Ok(result.Value);
    }

    [HttpPost("invitations/{token}/accept")]
    public async Task<IActionResult> AcceptInvitation(string token, AcceptInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AcceptInvitationCommand(token, request.PetId), cancellationToken);
        return result.IsFailure ? MapError(result.Error) : Ok(result.Value);
    }

    [HttpPost("invitations/{token}/reject")]
    public async Task<IActionResult> RejectInvitation(string token,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RejectInvitationCommand(token), cancellationToken);
        return result.IsFailure ? MapError(result.Error) : NoContent();
    }

    [HttpPost("invitations/{invitationId:guid}/cancel")]
    public async Task<IActionResult> CancelInvitation(Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelInvitationCommand(invitationId), cancellationToken);
        return result.IsFailure ? MapError(result.Error) : NoContent();
    }

    [HttpGet("invitations/mine")]
    public async Task<IActionResult> GetMine([FromQuery] string? direction,
        [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyInvitationsQuery(direction, status), cancellationToken);
        return result.IsFailure ? MapError(result.Error) : Ok(result.Value);
    }

    [HttpGet("pets/{petId:guid}/tree")]
    public async Task<IActionResult> GetTree(Guid petId, [FromQuery] int generations = 3,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetRelationshipTreeQuery(petId, generations), cancellationToken);
        return result.IsFailure ? MapError(result.Error) : Ok(result.Value);
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "GENEALOGY_PETS_SERVICE_UNAVAILABLE" =>
            StatusCode(StatusCodes.Status503ServiceUnavailable, error),
        _ => MapByType(error)
    };

    private IActionResult MapByType(Error error) => error.Type switch
    {
        ErrorType.NotFound => NotFound(error),
        ErrorType.Unauthorized => StatusCode(StatusCodes.Status403Forbidden, error),
        ErrorType.Conflict => Conflict(error),
        ErrorType.Validation => BadRequest(error),
        _ => BadRequest(error)
    };
}

public sealed record AddOwnParentRequest(Guid ParentPetId, string ParentRole);
public sealed record CreateInvitationRequest(Guid ChildPetId, string ParentRole, string OwnerEmail);
public sealed record AcceptInvitationRequest(Guid PetId);
