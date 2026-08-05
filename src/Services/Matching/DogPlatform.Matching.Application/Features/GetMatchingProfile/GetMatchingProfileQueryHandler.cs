using DogPlatform.Matching.Application.Features.UpsertMatchingProfile;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.GetMatchingProfile;

public sealed class GetMatchingProfileQueryHandler
    : IRequestHandler<GetMatchingProfileQuery, Result<MatchingProfileResponse>>
{
    private readonly IMatchingProfileRepository _profileRepository;
    private readonly ICurrentUser _currentUser;

    public GetMatchingProfileQueryHandler(
        IMatchingProfileRepository profileRepository, ICurrentUser currentUser)
    {
        _profileRepository = profileRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<MatchingProfileResponse>> Handle(
        GetMatchingProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByPetIdAsync(request.PetId, cancellationToken);

        if (profile is null)
            return Result.Failure<MatchingProfileResponse>(MatchingErrors.ProfileNotFound);

        if (profile.OwnerId != _currentUser.UserId)
            return Result.Failure<MatchingProfileResponse>(MatchingErrors.Unauthorized);

        return Result.Success(new MatchingProfileResponse(
            profile.Id,
            profile.PetId,
            profile.IsActive,
            profile.BreedPreferences.Select(bp => bp.BreedId).ToList(),
            profile.MinimumAgeMonths,
            profile.MaximumAgeMonths,
            profile.RequirePedigree,
            profile.RequireGenealogyValidation,
            profile.MaximumEstimatedInbreedingCoefficient,
            profile.MinimumCompatibilityScore,
            profile.CreatedAt,
            profile.UpdatedAt));
    }
}
