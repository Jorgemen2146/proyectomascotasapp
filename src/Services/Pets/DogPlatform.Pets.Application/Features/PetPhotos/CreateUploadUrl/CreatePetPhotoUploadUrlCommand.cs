using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.CreateUploadUrl;

public sealed record CreatePetPhotoUploadUrlCommand(
    Guid PetId,
    string FileName,
    string ContentType,
    long FileSize)
    : IRequest<Result<PetPhotoUploadUrlResponse>>;
