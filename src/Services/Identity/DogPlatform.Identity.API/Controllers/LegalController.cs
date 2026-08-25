using System.Security.Claims;
using DogPlatform.Identity.API.Requests.Authentication;
using DogPlatform.Identity.Application.Features.Legal;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Identity.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class LegalController(IMediator mediator) : ControllerBase
{
    [HttpGet("legal/documents")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveDocuments(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetActiveLegalDocumentsQuery(), cancellationToken));

    [HttpGet("me/legal-status")]
    [Authorize]
    public async Task<IActionResult> GetMyLegalStatus(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await mediator.Send(new GetLegalStatusQuery(userId), cancellationToken));
    }

    [HttpGet("me/legal-consents")]
    [Authorize]
    public async Task<IActionResult> GetMyLegalConsents(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await mediator.Send(new GetLegalConsentHistoryQuery(userId), cancellationToken));
    }

    [HttpPost("me/legal-consents")]
    [Authorize]
    public async Task<IActionResult> AcceptLegalDocument(
        [FromBody] AcceptLegalConsentRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await mediator.Send(
            new AcceptLegalConsentCommand(userId, request.LegalDocumentId), cancellationToken);
        if (result.IsSuccess) return Ok(result.Value);

        return result.Error.Type switch
        {
            ErrorType.NotFound => NotFound(result.Error),
            ErrorType.Conflict => Conflict(result.Error),
            _ => BadRequest(result.Error)
        };
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }
}
