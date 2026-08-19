using System.Security.Claims;
using DogPlatform.Identity.API.Requests.Authentication;
using DogPlatform.Identity.Application.Features.Authentication.Login;
using DogPlatform.Identity.Application.Features.Authentication.Logout;
using DogPlatform.Identity.Application.Features.Authentication.RefreshToken;
using DogPlatform.Identity.Application.Features.Authentication.Register;
using DogPlatform.Identity.Application.Features.Authentication.ResendVerification;
using DogPlatform.Identity.Application.Features.Authentication.VerifyEmail;
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
            request.PhoneNumber);

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
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        var email = User.FindFirstValue(ClaimTypes.Email)
                 ?? User.FindFirstValue("email");
        var firstName = User.FindFirstValue(ClaimTypes.GivenName)
                     ?? User.FindFirstValue("given_name");
        var lastName = User.FindFirstValue(ClaimTypes.Surname)
                    ?? User.FindFirstValue("family_name");

        return Ok(new
        {
            userId,
            email,
            firstName,
            lastName
        });
    }
}
