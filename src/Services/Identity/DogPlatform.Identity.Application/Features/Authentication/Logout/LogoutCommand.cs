using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Authentication.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
