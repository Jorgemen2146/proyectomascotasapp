using DogPlatform.SharedKernel.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Health.API.Controllers;

public abstract class HealthApiControllerBase : ControllerBase
{
    protected IActionResult FromError(Error error) => error.Type switch
    {
        ErrorType.NotFound => NotFound(error),
        ErrorType.Conflict => Conflict(error),
        ErrorType.Unauthorized when error.Code == "Vaccination.PetAuthenticationFailed" => Unauthorized(error),
        ErrorType.Unauthorized => Forbid(),
        ErrorType.Validation => BadRequest(error),
        _ when error.Code == "Vaccination.PetsServiceUnavailable" => StatusCode(StatusCodes.Status503ServiceUnavailable, error),
        _ => BadRequest(error)
    };
}
