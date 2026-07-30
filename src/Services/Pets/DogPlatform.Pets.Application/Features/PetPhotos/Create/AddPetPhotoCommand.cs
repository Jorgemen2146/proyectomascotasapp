using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.Create;

public sealed record AddPetPhotoCommand(Guid PetId, string ImageUrl)
    : IRequest<Result<PetPhotoResponse>>;
