using DogPlatform.Identity.Application.ProfilePhotos;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Profile.Photo;

public sealed class GetProfilePhotoContentQueryHandler
    : IRequestHandler<GetProfilePhotoContentQuery, Result<ProfilePhotoContent>>
{
    private readonly IUserRepository _userRepository;
    private readonly IProfilePhotoStorage _storage;

    public GetProfilePhotoContentQueryHandler(
        IUserRepository userRepository,
        IProfilePhotoStorage storage)
    {
        _userRepository = userRepository;
        _storage = storage;
    }

    public async Task<Result<ProfilePhotoContent>> Handle(
        GetProfilePhotoContentQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<ProfilePhotoContent>(UserErrors.NotFound);

        if (string.IsNullOrWhiteSpace(user.ProfilePhotoUrl))
            return Result.Failure<ProfilePhotoContent>(Error.NotFound(
                "Profile.Photo.NotFound", "The user does not have a profile photo."));

        return await _storage.OpenReadAsync(user.ProfilePhotoUrl, cancellationToken);
    }
}
