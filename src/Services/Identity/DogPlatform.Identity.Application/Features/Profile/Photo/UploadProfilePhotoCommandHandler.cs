using DogPlatform.Identity.Application.ProfilePhotos;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Profile.Photo;

public sealed class UploadProfilePhotoCommandHandler
    : IRequestHandler<UploadProfilePhotoCommand, Result<ProfilePhotoResponse>>
{
    private const int MaximumImageBytes = 10 * 1024 * 1024;
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IProfilePhotoStorage _storage;
    private readonly TimeProvider _timeProvider;

    public UploadProfilePhotoCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IProfilePhotoStorage storage,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _storage = storage;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ProfilePhotoResponse>> Handle(
        UploadProfilePhotoCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<ProfilePhotoResponse>(UserErrors.NotFound);

        byte[] content;
        try
        {
            content = Convert.FromBase64String(request.ImageBase64);
        }
        catch (FormatException)
        {
            return Result.Failure<ProfilePhotoResponse>(Error.Validation(
                "Profile.Photo.InvalidBase64", "ImageBase64 is not valid Base64."));
        }

        if (content.Length == 0 || content.Length > MaximumImageBytes)
            return Result.Failure<ProfilePhotoResponse>(Error.Validation(
                "Profile.Photo.InvalidSize", "The decoded image must be between 1 byte and 10 MB."));

        var stored = await _storage.SaveAsync(
            request.UserId,
            content,
            request.ContentType,
            request.FileName,
            cancellationToken);
        if (stored.IsFailure)
            return Result.Failure<ProfilePhotoResponse>(stored.Error);

        var previousObjectKey = user.ProfilePhotoUrl;
        user.SetProfilePhotoUrl(stored.Value.ObjectKey, _timeProvider.GetUtcNow().UtcDateTime);

        try
        {
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(stored.Value.ObjectKey, CancellationToken.None);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousObjectKey))
            await _storage.DeleteAsync(previousObjectKey, CancellationToken.None);

        return Result.Success(new ProfilePhotoResponse(ProfilePhotoUrls.Content));
    }
}
