using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Domain.Aggregates.Pet;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.Create;

public sealed class AddPetPhotoCommandHandler
    : IRequestHandler<AddPetPhotoCommand, Result<PetPhotoResponse>>
{
    private readonly IPetRepository _petRepository;
    private readonly IPetPhotoRepository _photoRepository;
    private readonly IPetsUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AddPetPhotoCommandHandler(
        IPetRepository petRepository,
        IPetPhotoRepository photoRepository,
        IPetsUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _petRepository = petRepository;
        _photoRepository = photoRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PetPhotoResponse>> Handle(
        AddPetPhotoCommand request,
        CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<PetPhotoResponse>(PetErrors.NotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure<PetPhotoResponse>(PetErrors.Unauthorized);

        if (pet.IsDeleted)
            return Result.Failure<PetPhotoResponse>(PetErrors.AlreadyDeleted);

        var existingPhotos = await _photoRepository.GetByPetIdAsync(request.PetId, cancellationToken);
        bool isMain = !existingPhotos.Any();

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var photo = PetPhoto.Create(
            Guid.NewGuid(),
            request.PetId,
            request.ImageUrl,
            isMain,
            now);

        await _photoRepository.AddAsync(photo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PetPhotoResponse(
            photo.Id,
            photo.PetId,
            photo.Url,
            photo.IsMain,
            photo.CreatedAt));
    }
}
