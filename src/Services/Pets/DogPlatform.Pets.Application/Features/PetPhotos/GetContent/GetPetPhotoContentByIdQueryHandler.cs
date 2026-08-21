using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.GetContent;

public sealed class GetPetPhotoContentByIdQueryHandler
    : IRequestHandler<GetPetPhotoContentByIdQuery, Result<PhotoContent>>
{
    private readonly IPetRepository _pets;
    private readonly IPetPhotoRepository _photos;
    private readonly IPhotoStorageService _storage;
    private readonly ICurrentUser _currentUser;

    public GetPetPhotoContentByIdQueryHandler(
        IPetRepository pets,
        IPetPhotoRepository photos,
        IPhotoStorageService storage,
        ICurrentUser currentUser)
    {
        _pets = pets;
        _photos = photos;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<Result<PhotoContent>> Handle(
        GetPetPhotoContentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var pet = await _pets.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null || pet.IsDeleted)
            return Result.Failure<PhotoContent>(PetErrors.NotFound);
        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure<PhotoContent>(PetErrors.Unauthorized);

        var photo = await _photos.GetByIdAsync(request.PhotoId, cancellationToken);
        if (photo is null || photo.PetId != request.PetId)
            return Result.Failure<PhotoContent>(PetErrors.PhotoNotFound);

        return await _storage.OpenReadAsync(photo.Url, cancellationToken);
    }
}
