using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Profile.Photo;

public sealed record UploadProfilePhotoCommand(
    Guid UserId,
    string FileName,
    string ContentType,
    string ImageBase64) : IRequest<Result<ProfilePhotoResponse>>;

public sealed record ProfilePhotoResponse(string ProfilePhotoUrl);
