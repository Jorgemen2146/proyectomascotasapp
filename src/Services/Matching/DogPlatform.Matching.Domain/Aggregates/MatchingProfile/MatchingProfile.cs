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
        DateTime createdAt)
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
        DateTime utcNow)
    {
        var validation = Validate(
            minimumAgeMonths,
            maximumAgeMonths,
            maximumEstimatedInbreedingCoefficient,
            minimumCompatibilityScore);

        if (validation.IsFailure)
            return Result.Failure<MatchingProfile>(validation.Error);

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
            utcNow);

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
        DateTime utcNow)
    {
        var validation = Validate(
            minimumAgeMonths,
            maximumAgeMonths,
            maximumEstimatedInbreedingCoefficient,
            minimumCompatibilityScore);

        if (validation.IsFailure)
            return validation;

        IsActive = isActive;
        MinimumAgeMonths = minimumAgeMonths;
        MaximumAgeMonths = maximumAgeMonths;
        RequirePedigree = requirePedigree;
        RequireGenealogyValidation = requireGenealogyValidation;
        MaximumEstimatedInbreedingCoefficient = maximumEstimatedInbreedingCoefficient;
        MinimumCompatibilityScore = minimumCompatibilityScore;
        UpdatedAt = utcNow;

        _breedPreferences.Clear();
        foreach (var breedId in preferredBreedIds.Distinct())
            _breedPreferences.Add(MatchingProfileBreedPreference.Create(Id, breedId));

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
