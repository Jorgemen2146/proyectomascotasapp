using DogPlatform.Pets.Application.Storage;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.GetContent;

public sealed record GetPetPhotoContentQuery(Guid PetId, string ObjectKey)
    : IRequest<Result<PhotoContent>>;
