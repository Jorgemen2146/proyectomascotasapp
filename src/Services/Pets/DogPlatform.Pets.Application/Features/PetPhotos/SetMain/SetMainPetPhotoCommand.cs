using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.SetMain;

public sealed record SetMainPetPhotoCommand(Guid PetId, Guid PhotoId)
    : IRequest<Result>;
