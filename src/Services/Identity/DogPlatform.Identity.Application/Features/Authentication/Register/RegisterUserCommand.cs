using DogPlatform.SharedKernel.Primitives;
using DogPlatform.Identity.Application.Features.Legal;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Authentication.Register;

public sealed record RegisterUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber = null,
    IReadOnlyList<LegalConsentSelection>? LegalConsents = null)
    : IRequest<Result<RegisterUserResponse>>;
