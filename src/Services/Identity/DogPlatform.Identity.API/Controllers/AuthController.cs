using DogPlatform.Identity.API.Requests.Authentication;
using DogPlatform.Identity.Application.Features.Authentication.Register;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
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
}
