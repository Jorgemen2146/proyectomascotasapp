using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Domain.Aggregates.Pet;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.ConfirmUpload;

public sealed class ConfirmPetPhotoUploadCommandHandler
    : IRequestHandler<ConfirmPetPhotoUploadCommand, Result<PetPhotoResponse>>
{
    private readonly IPetRepository _petRepository;
    private readonly IPetPhotoRepository _photoRepository;
    private readonly IPhotoStorageService _storageService;
    private readonly IPetsUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ConfirmPetPhotoUploadCommandHandler(
        IPetRepository petRepository,
        IPetPhotoRepository photoRepository,
        IPhotoStorageService storageService,
        IPetsUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _petRepository = petRepository;
        _photoRepository = photoRepository;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PetPhotoResponse>> Handle(
        ConfirmPetPhotoUploadCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate ownership
        var pet = await _petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<PetPhotoResponse>(PetErrors.NotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure<PetPhotoResponse>(PetErrors.Unauthorized);

        if (pet.IsDeleted)
            return Result.Failure<PetPhotoResponse>(PetErrors.AlreadyDeleted);

        // 2. Verify object key belongs to this user and pet
        var expectedPrefix = $"pets/{_currentUser.UserId}/{request.PetId}/";
        if (!request.ObjectKey.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            return Result.Failure<PetPhotoResponse>(PetErrors.InvalidObjectKey);

        // 3. Verify object exists in the active storage provider
        var exists = await _storageService.ObjectExistsAsync(request.ObjectKey, cancellationToken);
        if (!exists)
            return Result.Failure<PetPhotoResponse>(PetErrors.ObjectNotFound);

        // 4. Derive the canonical URL stored in DB (object key is source of truth)
        // Since the table has only Url, store the object key as the URL value.
        // If a public base URL is configured, the infrastructure layer should expose a
        // separate resolution method. For now the object key is the canonical reference.
        // TODO: if PublicBaseUrl is needed, extend S3PhotoStorageService to build the public URL
        // and pass it here via a new IPhotoStorageService method.
        var canonicalUrl = request.ObjectKey;

        // 5. Reject duplicate registration
        var duplicate = await _photoRepository.ExistsByUrlAsync(request.PetId, canonicalUrl, cancellationToken);
        if (duplicate)
            return Result.Failure<PetPhotoResponse>(PetErrors.DuplicatePhoto);

        // 6. Determine if this will be the main photo
        var existingPhotos = await _photoRepository.GetByPetIdAsync(request.PetId, cancellationToken);
        bool isMain = !existingPhotos.Any();

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var photo = PetPhoto.Create(Guid.NewGuid(), request.PetId, canonicalUrl, isMain, now);

        await _photoRepository.AddAsync(photo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PetPhotoResponse(
            photo.Id,
            photo.PetId,
            PetPhotoUrls.Content(photo.PetId, photo.Id),
            photo.IsMain,
            photo.CreatedAt));
    }
}
