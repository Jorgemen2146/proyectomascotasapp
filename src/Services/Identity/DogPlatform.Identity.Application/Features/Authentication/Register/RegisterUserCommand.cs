using MediatR;

namespace DogPlatform.Identity.Application.Features.Authentication.Register;

public sealed record RegisterUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber = null) : IRequest<RegisterUserResponse>;
