using DogPlatform.SharedKernel.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Matching.API.Controllers;

public abstract class MatchingApiControllerBase : ControllerBase
{
    protected IActionResult FromError(Error error) => error.Type switch
    {
        ErrorType.NotFound => NotFound(error),
        ErrorType.Conflict => Conflict(error),
        ErrorType.Unauthorized => Forbid(),
        ErrorType.Validation => BadRequest(error),
        _ => BadRequest(error)
    };
}
