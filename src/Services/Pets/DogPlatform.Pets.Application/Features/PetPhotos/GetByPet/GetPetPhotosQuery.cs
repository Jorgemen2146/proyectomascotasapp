using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.GetByPet;

public sealed record GetPetPhotosQuery(Guid PetId)
    : IRequest<Result<IReadOnlyCollection<PetPhotoResponse>>>;
