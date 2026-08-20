using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Profile.GetMyProfile;

public sealed record GetMyProfileQuery(Guid UserId)
    : IRequest<Result<MyProfileResponse>>;
