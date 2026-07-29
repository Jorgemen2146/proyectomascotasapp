using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Authentication.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken) : IRequest<Result<RefreshTokenResponse>>;
