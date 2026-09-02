using System.Security.Claims;
using DogPlatform.Identity.API.Requests.Authentication;
using DogPlatform.Identity.Application.Features.Authentication.External;
using DogPlatform.Identity.Application.Features.Authentication.Login;
using DogPlatform.Identity.Application.Features.Legal;
using DogPlatform.Identity.Domain.Aggregates.ExternalLogin;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DogPlatform.Identity.API.Controllers;

[ApiController]
[Route("api/v1/auth/external")]
[EnableRateLimiting("external-auth")]
public sealed class ExternalAuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("google")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Google(GoogleExternalAuthRequest request,
        CancellationToken cancellationToken) => Authenticate(
            ExternalAuthProvider.Google, request.IdToken, null,
            Map(request.LegalConsents), cancellationToken);

    [HttpPost("facebook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Facebook(FacebookExternalAuthRequest request,
        CancellationToken cancellationToken) => Authenticate(
            ExternalAuthProvider.Facebook, request.AccessToken, null,
            Map(request.LegalConsents), cancellationToken);

    [HttpPost("apple")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Apple(AppleExternalAuthRequest request,
        CancellationToken cancellationToken) => Authenticate(
            ExternalAuthProvider.Apple, request.IdToken, request.Nonce,
            Map(request.LegalConsents), cancellationToken);

    [HttpPost("complete-registration")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteRegistration(
        CompleteExternalRegistrationRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CompleteExternalRegistrationCommand(
            request.RegistrationToken, request.Email, request.FirstName, request.LastName,
            Map(request.LegalConsents)), cancellationToken);
        return MapResult(result);
    }

    [HttpPost("{provider}/link")]
    [Authorize]
    public async Task<IActionResult> Link(string provider, LinkExternalLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ExternalAuthProvider>(provider, true, out var parsedProvider))
            return BadRequest(new { error = "EXTERNAL_PROVIDER_INVALID", description = "Unsupported external provider." });
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId)) return Unauthorized();
        var result = await mediator.Send(new LinkExternalLoginCommand(
            userId, parsedProvider, request.Credential, request.Nonce), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    private async Task<IActionResult> Authenticate(ExternalAuthProvider provider,
        string credential, string? nonce, IReadOnlyList<LegalConsentSelection>? legalConsents,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ExternalAuthCommand(
            provider, credential, nonce, legalConsents), cancellationToken);
        return MapResult(result);
    }

    private IActionResult MapResult(Result<ExternalAuthOutcome> result)
    {
        if (result.IsFailure) return MapError(result.Error);
        if (result.Value.IsAuthenticated) return Ok(result.Value.Session);
        return UnprocessableEntity(new
        {
            error = result.Value.ActionCode,
            description = "Additional action is required to complete external authentication.",
            registrationToken = result.Value.RegistrationToken,
            missingFields = result.Value.MissingFields
        });
    }

    private IActionResult MapError(Error error) => error.Type switch
    {
        ErrorType.Unauthorized => Unauthorized(new { error = error.Code, description = error.Description }),
        ErrorType.Conflict => Conflict(new { error = error.Code, description = error.Description }),
        ErrorType.Validation => UnprocessableEntity(new { error = error.Code, description = error.Description }),
        _ => StatusCode(StatusCodes.Status503ServiceUnavailable,
            new { error = error.Code, description = error.Description })
    };

    private static IReadOnlyList<LegalConsentSelection>? Map(
        IReadOnlyList<LegalConsentRequest>? consents) => consents?.Select(
            item => new LegalConsentSelection(item.Type, item.Version)).ToList();
}
