using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.Create;

public sealed record AddPetPhotoCommand(
    Guid PetId,
    string FileName,
    string ContentType,
    string ImageBase64)
    : IRequest<Result<PetPhotoResponse>>;
