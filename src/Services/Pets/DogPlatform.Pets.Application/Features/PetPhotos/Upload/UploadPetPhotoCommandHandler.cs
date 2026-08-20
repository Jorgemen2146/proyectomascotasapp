using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.Upload;

public sealed class UploadPetPhotoCommandHandler
    : IRequestHandler<UploadPetPhotoCommand, Result>
{
    private readonly IPetRepository _petRepository;
    private readonly IPhotoStorageService _storage;
    private readonly ICurrentUser _currentUser;

    public UploadPetPhotoCommandHandler(
        IPetRepository petRepository,
        IPhotoStorageService storage,
        ICurrentUser currentUser)
    {
        _petRepository = petRepository;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UploadPetPhotoCommand request,
        CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null || pet.IsDeleted)
            return Result.Failure(PetErrors.NotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure(PetErrors.Unauthorized);

        return await _storage.UploadObjectAsync(
            new PhotoUploadRequest(
                _currentUser.UserId,
                request.PetId,
                request.UploadToken,
                request.ContentType,
                request.ContentLength),
            request.Content,
            cancellationToken);
    }
}
