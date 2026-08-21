using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Application.Storage;
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
    private readonly IPhotoStorageService _storage;

    public AddPetPhotoCommandHandler(
        IPetRepository petRepository,
        IPetPhotoRepository photoRepository,
        IPetsUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IPhotoStorageService storage)
    {
        _petRepository = petRepository;
        _photoRepository = photoRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _storage = storage;
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

        byte[] content;
        try
        {
            content = Convert.FromBase64String(request.ImageBase64);
        }
        catch (FormatException)
        {
            return Result.Failure<PetPhotoResponse>(Error.Validation(
                "Pet.Photo.InvalidBase64", "ImageBase64 is not valid Base64."));
        }

        const int maximumImageBytes = 10 * 1024 * 1024;
        if (content.Length == 0 || content.Length > maximumImageBytes)
            return Result.Failure<PetPhotoResponse>(Error.Validation(
                "Pet.Photo.InvalidSize", "The decoded image must be between 1 byte and 10 MB."));

        var stored = await _storage.SaveAsync(
            request.PetId,
            content,
            request.ContentType,
            request.FileName,
            cancellationToken);
        if (stored.IsFailure)
            return Result.Failure<PetPhotoResponse>(stored.Error);

        var existingPhotos = await _photoRepository.GetByPetIdAsync(request.PetId, cancellationToken);
        bool isMain = !existingPhotos.Any();

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var photo = PetPhoto.Create(
            Guid.NewGuid(),
            request.PetId,
            stored.Value.ObjectKey,
            isMain,
            now);

        try
        {
            await _photoRepository.AddAsync(photo, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteObjectAsync(stored.Value.ObjectKey, CancellationToken.None);
            throw;
        }

        return Result.Success(new PetPhotoResponse(
            photo.Id,
            photo.PetId,
            PetPhotoUrls.Content(photo.PetId, photo.Id),
            photo.IsMain,
            photo.CreatedAt));
    }
}
