using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Errors;

public static class RefreshTokenErrors
{
    public static readonly Error NotFound =
        Error.NotFound("RefreshToken.NotFound", "The refresh token was not found.");

    public static readonly Error AlreadyRevoked =
        Error.Conflict("RefreshToken.AlreadyRevoked", "The refresh token has already been revoked.");

    public static readonly Error Expired =
        Error.Unauthorized("RefreshToken.Expired", "The refresh token has expired.");

    public static readonly Error Invalid =
        Error.Unauthorized("RefreshToken.Invalid", "The refresh token is invalid or has been revoked.");
}
