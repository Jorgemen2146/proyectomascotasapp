using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Authentication.ResendVerification;

public sealed record ResendVerificationCommand(string Email)
    : IRequest<Result<ResendVerificationResponse>>;
