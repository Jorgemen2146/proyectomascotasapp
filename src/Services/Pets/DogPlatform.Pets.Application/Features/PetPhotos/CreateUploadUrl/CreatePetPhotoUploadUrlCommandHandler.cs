using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.PetPhotos.CreateUploadUrl;

public sealed class CreatePetPhotoUploadUrlCommandHandler
    : IRequestHandler<CreatePetPhotoUploadUrlCommand, Result<PetPhotoUploadUrlResponse>>
{
    private readonly IPetRepository _petRepository;
    private readonly IPhotoStorageService _storageService;
    private readonly ICurrentUser _currentUser;

    public CreatePetPhotoUploadUrlCommandHandler(
        IPetRepository petRepository,
        IPhotoStorageService storageService,
        ICurrentUser currentUser)
    {
        _petRepository = petRepository;
        _storageService = storageService;
        _currentUser = currentUser;
    }

    public async Task<Result<PetPhotoUploadUrlResponse>> Handle(
        CreatePetPhotoUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<PetPhotoUploadUrlResponse>(PetErrors.NotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure<PetPhotoUploadUrlResponse>(PetErrors.Unauthorized);

        if (pet.IsDeleted)
            return Result.Failure<PetPhotoUploadUrlResponse>(PetErrors.AlreadyDeleted);

        var uploadResult = await _storageService.CreatePresignedUploadAsync(
            _currentUser.UserId,
            request.PetId,
            request.FileName,
            request.ContentType,
            request.FileSize,
            cancellationToken);

        if (uploadResult.IsFailure)
            return Result.Failure<PetPhotoUploadUrlResponse>(uploadResult.Error);

        var r = uploadResult.Value;
        return Result.Success(new PetPhotoUploadUrlResponse(
            r.ObjectKey,
            r.UploadUrl,
            r.Method,
            r.ExpiresAtUtc,
            r.RequiredHeaders));
    }
}
