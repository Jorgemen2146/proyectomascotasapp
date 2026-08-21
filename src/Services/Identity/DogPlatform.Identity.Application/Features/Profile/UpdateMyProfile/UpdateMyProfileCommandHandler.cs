using DogPlatform.Identity.Application.Features.Profile.GetMyProfile;
using DogPlatform.Identity.Application.Features.Profile.Photo;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using FluentValidation;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Profile.UpdateMyProfile;

public sealed class UpdateMyProfileCommandHandler
    : IRequestHandler<UpdateMyProfileCommand, Result<MyProfileResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<UpdateMyProfileCommand> _validator;

    public UpdateMyProfileCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<UpdateMyProfileCommand> validator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _validator = validator;
    }

    public async Task<Result<MyProfileResponse>> Handle(
        UpdateMyProfileCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure<MyProfileResponse>(Error.Validation(
                "Profile.Validation",
                string.Join(" ", validation.Errors.Select(error => error.ErrorMessage))));
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<MyProfileResponse>(UserErrors.NotFound);

        var updateResult = user.UpdateProfile(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            _timeProvider.GetUtcNow().UtcDateTime);

        if (updateResult.IsFailure)
            return Result.Failure<MyProfileResponse>(updateResult.Error);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new MyProfileResponse(
            user.Id,
            user.FullName.FirstName,
            user.FullName.LastName,
            user.Email.Value,
            user.PhoneNumber,
            user.IsEmailConfirmed,
            string.IsNullOrWhiteSpace(user.ProfilePhotoUrl) ? null : ProfilePhotoUrls.Content));
    }
}
