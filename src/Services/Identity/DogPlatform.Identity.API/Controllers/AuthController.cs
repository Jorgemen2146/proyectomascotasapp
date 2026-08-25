using System.Security.Claims;
using DogPlatform.Identity.API.Requests.Authentication;
using DogPlatform.Identity.Application.Features.Authentication.Login;
using DogPlatform.Identity.Application.Features.Authentication.Logout;
using DogPlatform.Identity.Application.Features.Authentication.RefreshToken;
using DogPlatform.Identity.Application.Features.Authentication.Register;
using DogPlatform.Identity.Application.Features.Authentication.ResendVerification;
using DogPlatform.Identity.Application.Features.Authentication.VerifyEmail;
using DogPlatform.Identity.Application.Features.Legal;
using DogPlatform.Identity.Application.Features.Profile.GetMyProfile;
using DogPlatform.Identity.Application.Features.Profile.UpdateMyProfile;
using DogPlatform.Identity.Application.Features.Profile.Photo;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Identity.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            request.PhoneNumber,
            request.LegalConsents?.Select(consent =>
                new LegalConsentSelection(consent.Type, consent.Version)).ToList());

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Conflict => Conflict(new { error = result.Error.Code, description = result.Error.Description }),
                ErrorType.Validation => BadRequest(new { error = result.Error.Code, description = result.Error.Description }),
                _ => BadRequest(new { error = result.Error.Code, description = result.Error.Description })
            };
        }

        return CreatedAtAction(
            nameof(Register),
            new { userId = result.Value.UserId },
            result.Value);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "EMAIL_NOT_VERIFIED")
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { error = result.Error.Code, description = result.Error.Description });
            }

            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(new { error = result.Error.Code, description = result.Error.Description }),
                ErrorType.Validation => BadRequest(new { error = result.Error.Code, description = result.Error.Description }),
                _ => BadRequest(new { error = result.Error.Code, description = result.Error.Description })
            };
        }

        return Ok(result.Value);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VerifyEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new VerifyEmailCommand(request.Email, request.Code),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Conflict => Conflict(new { error = result.Error.Code, description = result.Error.Description }),
                ErrorType.Validation => BadRequest(new { error = result.Error.Code, description = result.Error.Description }),
                _ => BadRequest(new { error = result.Error.Code, description = result.Error.Description })
            };
        }

        return Ok(result.Value);
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResendVerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ResendVerificationCommand(request.Email),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new
            {
                error = result.Error.Code,
                description = result.Error.Description
            });
        }

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(new { error = result.Error.Code, description = result.Error.Description }),
                ErrorType.Validation => BadRequest(new { error = result.Error.Code, description = result.Error.Description }),
                _ => BadRequest(new { error = result.Error.Code, description = result.Error.Description })
            };
        }

        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetMyProfileQuery(userId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(MyProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new UpdateMyProfileCommand(
            userId,
            request.FirstName,
            request.LastName,
            request.PhoneNumber), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    [HttpPost("me/photo")]
    [Authorize]
    [ProducesResponseType(typeof(ProfilePhotoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadMyPhoto(
        [FromBody] UploadProfilePhotoRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new UploadProfilePhotoCommand(
            userId, request.FileName, request.ContentType, request.ImageBase64), cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Type switch
        {
            ErrorType.NotFound => NotFound(result.Error),
            ErrorType.Validation => BadRequest(result.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result.Error)
        };
    }

    [HttpGet("me/photo/content")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyPhotoContent(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetProfilePhotoContentQuery(userId), cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == ErrorType.NotFound
                ? NotFound(result.Error)
                : StatusCode(StatusCodes.Status500InternalServerError, result.Error);

        return File(result.Value.Stream, result.Value.ContentType);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }
}
