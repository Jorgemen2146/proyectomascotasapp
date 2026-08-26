using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.UpsertMatchingProfile;

public sealed class UpsertMatchingProfileCommandHandler
    : IRequestHandler<UpsertMatchingProfileCommand, Result<MatchingProfileResponse>>
{
    private readonly IMatchingProfileRepository _profileRepository;
    private readonly IMatchingUnitOfWork _unitOfWork;
    private readonly IPetsMatchingClient _petsClient;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpsertMatchingProfileCommandHandler(
        IMatchingProfileRepository profileRepository,
        IMatchingUnitOfWork unitOfWork,
        IPetsMatchingClient petsClient,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _petsClient = petsClient;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MatchingProfileResponse>> Handle(
        UpsertMatchingProfileCommand request, CancellationToken cancellationToken)
    {
        var pet = await _petsClient.GetPetForMatchingAsync(request.PetId, cancellationToken);

        if (pet is null || pet.IsDeleted)
            return Result.Failure<MatchingProfileResponse>(MatchingErrors.PetNotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure<MatchingProfileResponse>(MatchingErrors.Unauthorized);

        if (pet.IsSterilized)
            return Result.Failure<MatchingProfileResponse>(MatchingErrors.MatchingNotCompatible);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var existingProfile = await _profileRepository.GetByPetIdAsync(request.PetId, cancellationToken);

        if (existingProfile is null)
        {
            var creation = Domain.Aggregates.MatchingProfile.MatchingProfile.Create(
                request.PetId,
                _currentUser.UserId,
                request.IsActive,
                request.PreferredBreedIds,
                request.MinimumAgeMonths,
                request.MaximumAgeMonths,
                request.RequirePedigree,
                request.RequireGenealogyValidation,
                request.MaximumEstimatedInbreedingCoefficient,
                request.MinimumCompatibilityScore,
                utcNow,
                request.LookingForSex,
                request.AllowMixedBreed,
                request.Description,
                request.AvailableFromUtc);

            if (creation.IsFailure)
                return Result.Failure<MatchingProfileResponse>(creation.Error);

            _profileRepository.Add(creation.Value);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(Map(creation.Value));
        }

        var update = existingProfile.Update(
            request.IsActive,
            request.PreferredBreedIds,
            request.MinimumAgeMonths,
            request.MaximumAgeMonths,
            request.RequirePedigree,
            request.RequireGenealogyValidation,
            request.MaximumEstimatedInbreedingCoefficient,
            request.MinimumCompatibilityScore,
            utcNow,
            request.LookingForSex,
            request.AllowMixedBreed,
            request.Description,
            request.AvailableFromUtc);

        if (update.IsFailure)
            return Result.Failure<MatchingProfileResponse>(update.Error);

        _profileRepository.Update(existingProfile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(existingProfile));
    }

    private static MatchingProfileResponse Map(Domain.Aggregates.MatchingProfile.MatchingProfile profile) =>
        new(
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
            profile.UpdatedAt,
            profile.LookingForSex,
            profile.AllowMixedBreed,
            profile.Description,
            profile.AvailableFromUtc);
}
