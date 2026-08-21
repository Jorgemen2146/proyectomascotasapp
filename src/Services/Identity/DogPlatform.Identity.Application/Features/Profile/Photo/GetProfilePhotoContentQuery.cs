using DogPlatform.Identity.Application.ProfilePhotos;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Profile.Photo;

public sealed record GetProfilePhotoContentQuery(Guid UserId)
    : IRequest<Result<ProfilePhotoContent>>;
