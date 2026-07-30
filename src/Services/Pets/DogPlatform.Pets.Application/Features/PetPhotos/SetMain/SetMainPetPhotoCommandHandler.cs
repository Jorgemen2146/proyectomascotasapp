using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.SetMain;

public sealed class SetMainPetPhotoCommandHandler : IRequestHandler<SetMainPetPhotoCommand, Result>
{
    private readonly IPetRepository _petRepository;
    private readonly IPetPhotoRepository _photoRepository;
    private readonly IPetsUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public SetMainPetPhotoCommandHandler(
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

    public async Task<Result> Handle(SetMainPetPhotoCommand request, CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByIdWithPhotosAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure(PetErrors.NotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure(PetErrors.Unauthorized);

        var photo = await _photoRepository.GetByIdAsync(request.PhotoId, cancellationToken);
        if (photo is null || photo.PetId != request.PetId)
            return Result.Failure(PetErrors.PhotoNotFound);

        var result = pet.SetMainPhoto(request.PhotoId);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
