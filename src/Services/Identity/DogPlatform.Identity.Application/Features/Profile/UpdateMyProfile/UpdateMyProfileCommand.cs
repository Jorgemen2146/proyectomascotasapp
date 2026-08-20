using DogPlatform.Identity.Application.Features.Profile.GetMyProfile;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Profile.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber)
    : IRequest<Result<MyProfileResponse>>;
