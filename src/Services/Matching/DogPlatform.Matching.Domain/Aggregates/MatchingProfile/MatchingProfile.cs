using DogPlatform.Matching.Domain.Errors;
using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Matching.Domain.Aggregates.MatchingProfile;

/// <summary>
/// Represents a pet owner's opt-in configuration to appear as a candidate for
/// breeding matches, along with their search preferences. Does not store a copy
/// of the Pet aggregate — only the minimal configuration needed by Matching.
/// </summary>
public sealed class MatchingProfile : AggregateRoot<Guid>
{
    private readonly List<MatchingProfileBreedPreference> _breedPreferences = [];

    private MatchingProfile(
        Guid id,
        Guid petId,
        Guid ownerId,
        bool isActive,
        int minimumAgeMonths,
        int maximumAgeMonths,
        bool requirePedigree,
        bool requireGenealogyValidation,
        double maximumEstimatedInbreedingCoefficient,
        int minimumCompatibilityScore,
        DateTime createdAt,
        string? lookingForSex,
        bool allowMixedBreed,
        string? description,
        DateTime? availableFromUtc)
        : base(id)
    {
        PetId = petId;
        OwnerId = ownerId;
        IsActive = isActive;
        MinimumAgeMonths = minimumAgeMonths;
        MaximumAgeMonths = maximumAgeMonths;
        RequirePedigree = requirePedigree;
        RequireGenealogyValidation = requireGenealogyValidation;
        MaximumEstimatedInbreedingCoefficient = maximumEstimatedInbreedingCoefficient;
        MinimumCompatibilityScore = minimumCompatibilityScore;
        CreatedAt = createdAt;
        LookingForSex = lookingForSex;
        AllowMixedBreed = allowMixedBreed;
        Description = description;
        AvailableFromUtc = availableFromUtc;
    }

    private MatchingProfile() { }

    public Guid PetId { get; private set; }
    public Guid OwnerId { get; private set; }
    public bool IsActive { get; private set; }
    public int MinimumAgeMonths { get; private set; }
    public int MaximumAgeMonths { get; private set; }
    public bool RequirePedigree { get; private set; }
    public bool RequireGenealogyValidation { get; private set; }
    public double MaximumEstimatedInbreedingCoefficient { get; private set; }
    public int MinimumCompatibilityScore { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? LookingForSex { get; private set; }
    public bool AllowMixedBreed { get; private set; }
    public string? Description { get; private set; }
    public DateTime? AvailableFromUtc { get; private set; }

    public IReadOnlyCollection<MatchingProfileBreedPreference> BreedPreferences =>
        _breedPreferences.AsReadOnly();

    public static Result<MatchingProfile> Create(
        Guid petId,
        Guid ownerId,
        bool isActive,
        IEnumerable<int> preferredBreedIds,
        int minimumAgeMonths,
        int maximumAgeMonths,
        bool requirePedigree,
        bool requireGenealogyValidation,
        double maximumEstimatedInbreedingCoefficient,
        int minimumCompatibilityScore,
        DateTime utcNow,
        string? lookingForSex = null,
        bool allowMixedBreed = true,
        string? description = null,
        DateTime? availableFromUtc = null)
    {
        var validation = Validate(
            minimumAgeMonths,
            maximumAgeMonths,
            maximumEstimatedInbreedingCoefficient,
            minimumCompatibilityScore);

        if (validation.IsFailure)
            return Result.Failure<MatchingProfile>(validation.Error);

        if (lookingForSex is not null && !lookingForSex.Equals("M", StringComparison.OrdinalIgnoreCase)
            && !lookingForSex.Equals("F", StringComparison.OrdinalIgnoreCase))
            return Result.Failure<MatchingProfile>(MatchingErrors.InvalidLookingForSex);
        if (description is { Length: > 1000 })
            return Result.Failure<MatchingProfile>(MatchingErrors.ProfileDescriptionTooLong);

        var profile = new MatchingProfile(
            Guid.NewGuid(),
            petId,
            ownerId,
            isActive,
            minimumAgeMonths,
            maximumAgeMonths,
            requirePedigree,
            requireGenealogyValidation,
            maximumEstimatedInbreedingCoefficient,
            minimumCompatibilityScore,
            utcNow,
            lookingForSex?.ToUpperInvariant(),
            allowMixedBreed,
            description?.Trim(),
            availableFromUtc);

        foreach (var breedId in preferredBreedIds.Distinct())
            profile._breedPreferences.Add(
                MatchingProfileBreedPreference.Create(profile.Id, breedId));

        return Result.Success(profile);
    }

    public Result Update(
        bool isActive,
        IEnumerable<int> preferredBreedIds,
        int minimumAgeMonths,
        int maximumAgeMonths,
        bool requirePedigree,
        bool requireGenealogyValidation,
        double maximumEstimatedInbreedingCoefficient,
        int minimumCompatibilityScore,
        DateTime utcNow,
        string? lookingForSex = null,
        bool allowMixedBreed = true,
        string? description = null,
        DateTime? availableFromUtc = null)
    {
        var validation = Validate(
            minimumAgeMonths,
            maximumAgeMonths,
            maximumEstimatedInbreedingCoefficient,
            minimumCompatibilityScore);

        if (validation.IsFailure)
            return validation;

        if (lookingForSex is not null && !lookingForSex.Equals("M", StringComparison.OrdinalIgnoreCase)
            && !lookingForSex.Equals("F", StringComparison.OrdinalIgnoreCase))
            return Result.Failure(MatchingErrors.InvalidLookingForSex);
        if (description is { Length: > 1000 })
            return Result.Failure(MatchingErrors.ProfileDescriptionTooLong);

        IsActive = isActive;
        MinimumAgeMonths = minimumAgeMonths;
        MaximumAgeMonths = maximumAgeMonths;
        RequirePedigree = requirePedigree;
        RequireGenealogyValidation = requireGenealogyValidation;
        MaximumEstimatedInbreedingCoefficient = maximumEstimatedInbreedingCoefficient;
        MinimumCompatibilityScore = minimumCompatibilityScore;
        UpdatedAt = utcNow;
        LookingForSex = lookingForSex?.ToUpperInvariant();
        AllowMixedBreed = allowMixedBreed;
        Description = description?.Trim();
        AvailableFromUtc = availableFromUtc;

        _breedPreferences.Clear();
        foreach (var breedId in preferredBreedIds.Distinct())
            _breedPreferences.Add(MatchingProfileBreedPreference.Create(Id, breedId));

        return Result.Success();
    }

    public Result Deactivate(Guid ownerId, DateTime utcNow)
    {
        if (ownerId != OwnerId)
            return Result.Failure(MatchingErrors.Forbidden);
        IsActive = false;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    private static Result Validate(
        int minimumAgeMonths,
        int maximumAgeMonths,
        double maximumEstimatedInbreedingCoefficient,
        int minimumCompatibilityScore)
    {
        if (minimumAgeMonths > maximumAgeMonths)
            return Result.Failure(MatchingErrors.InvalidAgeRange);

        if (maximumEstimatedInbreedingCoefficient is < 0 or > 1)
            return Result.Failure(MatchingErrors.InvalidInbreedingCoefficient);

        if (minimumCompatibilityScore is < 0 or > 100)
            return Result.Failure(MatchingErrors.InvalidScore);

        return Result.Success();
    }
}
