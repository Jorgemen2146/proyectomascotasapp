using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Profile.GetMyProfile;

public sealed class GetMyProfileQueryHandler
    : IRequestHandler<GetMyProfileQuery, Result<MyProfileResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetMyProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<MyProfileResponse>> Handle(
        GetMyProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<MyProfileResponse>(UserErrors.NotFound);

        return Result.Success(new MyProfileResponse(
            user.Id,
            user.FullName.FirstName,
            user.FullName.LastName,
            user.Email.Value,
            user.PhoneNumber,
            user.IsEmailConfirmed));
    }
}
