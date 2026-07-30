using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.Delete;

public sealed record DeletePetPhotoCommand(Guid PetId, Guid PhotoId)
    : IRequest<Result>;
