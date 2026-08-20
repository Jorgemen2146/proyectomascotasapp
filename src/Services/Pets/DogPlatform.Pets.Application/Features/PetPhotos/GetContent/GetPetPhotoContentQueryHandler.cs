using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.GetContent;

public sealed class GetPetPhotoContentQueryHandler
    : IRequestHandler<GetPetPhotoContentQuery, Result<PhotoContent>>
{
    private readonly IPetRepository _petRepository;
    private readonly IPetPhotoRepository _photoRepository;
    private readonly IPhotoStorageService _storage;
    private readonly ICurrentUser _currentUser;

    public GetPetPhotoContentQueryHandler(
        IPetRepository petRepository,
        IPetPhotoRepository photoRepository,
        IPhotoStorageService storage,
        ICurrentUser currentUser)
    {
        _petRepository = petRepository;
        _photoRepository = photoRepository;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<Result<PhotoContent>> Handle(
        GetPetPhotoContentQuery request,
        CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null || pet.IsDeleted)
            return Result.Failure<PhotoContent>(PetErrors.NotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure<PhotoContent>(PetErrors.Unauthorized);

        var registered = await _photoRepository.ExistsByUrlAsync(
            request.PetId,
            request.ObjectKey,
            cancellationToken);
        if (!registered)
            return Result.Failure<PhotoContent>(PetErrors.PhotoNotFound);

        return await _storage.OpenReadAsync(request.ObjectKey, cancellationToken);
    }
}
