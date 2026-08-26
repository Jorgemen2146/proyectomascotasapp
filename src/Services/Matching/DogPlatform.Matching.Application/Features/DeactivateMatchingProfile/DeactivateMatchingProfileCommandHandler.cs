using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.DeactivateMatchingProfile;

public sealed class DeactivateMatchingProfileCommandHandler(
    IMatchingProfileRepository profiles,
    IMatchingUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<DeactivateMatchingProfileCommand, Result>
{
    public async Task<Result> Handle(DeactivateMatchingProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await profiles.GetByIdAsync(request.MatchingProfileId, cancellationToken);
        if (profile is null) return Result.Failure(MatchingErrors.MatchingProfileNotFound);
        var result = profile.Deactivate(currentUser.UserId, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure) return result;
        profiles.Update(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
