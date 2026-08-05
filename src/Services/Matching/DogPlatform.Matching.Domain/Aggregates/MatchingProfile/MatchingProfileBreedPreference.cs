using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Matching.Domain.Aggregates.MatchingProfile;

/// <summary>
/// Preferred breed for a matching profile. A profile with no preferences
/// accepts any breed as a candidate.
/// </summary>
public sealed class MatchingProfileBreedPreference : Entity<Guid>
{
    private MatchingProfileBreedPreference(Guid id, Guid matchingProfileId, int breedId)
        : base(id)
    {
        MatchingProfileId = matchingProfileId;
        BreedId = breedId;
    }

    private MatchingProfileBreedPreference() { }

    public Guid MatchingProfileId { get; private set; }
    public int BreedId { get; private set; }

    public static MatchingProfileBreedPreference Create(Guid matchingProfileId, int breedId) =>
        new(Guid.NewGuid(), matchingProfileId, breedId);
}
