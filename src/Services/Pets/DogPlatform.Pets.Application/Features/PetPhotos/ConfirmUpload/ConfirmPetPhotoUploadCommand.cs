using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.ConfirmUpload;

public sealed record ConfirmPetPhotoUploadCommand(Guid PetId, string ObjectKey)
    : IRequest<Result<PetPhotoResponse>>;
