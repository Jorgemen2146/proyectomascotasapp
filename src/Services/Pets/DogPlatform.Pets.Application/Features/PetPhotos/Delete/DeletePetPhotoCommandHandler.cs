using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DogPlatform.Pets.Application.Features.PetPhotos.Delete;

public sealed class DeletePetPhotoCommandHandler : IRequestHandler<DeletePetPhotoCommand, Result>
{
    private readonly IPetRepository _petRepository;
    private readonly IPetPhotoRepository _photoRepository;
    private readonly IPhotoStorageService _storageService;
    private readonly IPetsUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeletePetPhotoCommandHandler> _logger;

    public DeletePetPhotoCommandHandler(
        IPetRepository petRepository,
        IPetPhotoRepository photoRepository,
        IPhotoStorageService storageService,
        IPetsUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<DeletePetPhotoCommandHandler> logger)
    {
        _petRepository = petRepository;
        _photoRepository = photoRepository;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(DeletePetPhotoCommand request, CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByIdWithPhotosAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure(PetErrors.NotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure(PetErrors.Unauthorized);

        var photo = pet.Photos.FirstOrDefault(candidate => candidate.Id == request.PhotoId);
        if (photo is null)
            return Result.Failure(PetErrors.PhotoNotFound);

        // Capture the object key before removal
        var objectKey = photo.Url;

        var domainResult = pet.RemovePhoto(request.PhotoId);
        if (domainResult.IsFailure)
            return domainResult;

        // Database is the source of truth — commit first
        await _photoRepository.RemoveAsync(photo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Attempt provider cleanup after successful DB commit.
        // A failure here does NOT roll back the DB deletion.
        // TODO: for reliable eventual cleanup, enqueue an outbox event instead.
        var deleted = await _storageService.DeleteObjectAsync(objectKey, cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning(
                "Storage object deletion failed for key {ObjectKey} after DB photo {PhotoId} was removed. Manual cleanup may be required.",
                objectKey,
                request.PhotoId);
        }

        return Result.Success();
    }
}

