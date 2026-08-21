using DogPlatform.Pets.Application.Storage;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.GetContent;

public sealed record GetPetPhotoContentByIdQuery(Guid PetId, Guid PhotoId)
    : IRequest<Result<PhotoContent>>;
