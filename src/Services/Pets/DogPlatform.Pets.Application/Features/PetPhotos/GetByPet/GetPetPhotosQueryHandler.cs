using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.GetByPet;

public sealed class GetPetPhotosQueryHandler
    : IRequestHandler<GetPetPhotosQuery, Result<IReadOnlyCollection<PetPhotoResponse>>>
{
    private readonly IPetRepository _petRepository;
    private readonly IPetPhotoRepository _photoRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPhotoStorageService _storage;

    public GetPetPhotosQueryHandler(
        IPetRepository petRepository,
        IPetPhotoRepository photoRepository,
        ICurrentUser currentUser,
        IPhotoStorageService storage)
    {
        _petRepository = petRepository;
        _photoRepository = photoRepository;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<Result<IReadOnlyCollection<PetPhotoResponse>>> Handle(
        GetPetPhotosQuery request,
        CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<IReadOnlyCollection<PetPhotoResponse>>(PetErrors.NotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure<IReadOnlyCollection<PetPhotoResponse>>(PetErrors.Unauthorized);

        var photos = await _photoRepository.GetByPetIdAsync(request.PetId, cancellationToken);

        var response = photos
            .OrderByDescending(p => p.IsMain)
            .ThenBy(p => p.CreatedAt)
            .Select(p => new PetPhotoResponse(
                p.Id,
                p.PetId,
                PetPhotoUrls.Content(p.PetId, p.Id),
                p.IsMain,
                p.CreatedAt))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyCollection<PetPhotoResponse>>(response);
    }
}
