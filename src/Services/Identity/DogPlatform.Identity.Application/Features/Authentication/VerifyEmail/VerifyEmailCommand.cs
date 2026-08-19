using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Authentication.VerifyEmail;

public sealed record VerifyEmailCommand(string Email, string Code)
    : IRequest<Result<VerifyEmailResponse>>;
