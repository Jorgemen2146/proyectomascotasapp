using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.Upload;

public sealed record UploadPetPhotoCommand(
    Guid PetId,
    string UploadToken,
    string ContentType,
    long? ContentLength,
    Stream Content)
    : IRequest<Result>;
