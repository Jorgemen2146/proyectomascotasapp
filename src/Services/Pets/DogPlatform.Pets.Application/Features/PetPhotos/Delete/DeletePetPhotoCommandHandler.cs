using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.Delete;

public sealed class DeletePetPhotoCommandHandler : IRequestHandler<DeletePetPhotoCommand, Result>
{
    private readonly IPetRepository _petRepository;
    private readonly IPetPhotoRepository _photoRepository;
    private readonly IPetsUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeletePetPhotoCommandHandler(
        IPetRepository petRepository,
        IPetPhotoRepository photoRepository,
        IPetsUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _petRepository = petRepository;
        _photoRepository = photoRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeletePetPhotoCommand request, CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByIdWithPhotosAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure(PetErrors.NotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure(PetErrors.Unauthorized);

        var photo = await _photoRepository.GetByIdAsync(request.PhotoId, cancellationToken);
        if (photo is null || photo.PetId != request.PetId)
            return Result.Failure(PetErrors.PhotoNotFound);

        var result = pet.RemovePhoto(request.PhotoId);
        if (result.IsFailure)
            return result;

        await _photoRepository.RemoveAsync(photo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
