using DogPlatform.Matching.Domain.Enums;
using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Matching.Domain.Aggregates.PetMatch;

public sealed class PetMatch : AggregateRoot<Guid>
{
    private PetMatch(Guid id, Guid matchRequestId, Guid pet1Id, Guid pet2Id,
        Guid owner1Id, Guid owner2Id, bool owner1SharePhoneNumber,
        bool owner2SharePhoneNumber, DateTime createdAtUtc) : base(id)
    {
        MatchRequestId = matchRequestId;
        Pet1Id = pet1Id;
        Pet2Id = pet2Id;
        Owner1Id = owner1Id;
        Owner2Id = owner2Id;
        Owner1SharePhoneNumber = owner1SharePhoneNumber;
        Owner2SharePhoneNumber = owner2SharePhoneNumber;
        Status = PetMatchStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    private PetMatch() { }

    public Guid MatchRequestId { get; private set; }
    public Guid Pet1Id { get; private set; }
    public Guid Pet2Id { get; private set; }
    public Guid Owner1Id { get; private set; }
    public Guid Owner2Id { get; private set; }
    public bool Owner1ShareDisplayName { get; private set; } = true;
    public bool Owner1SharePhoneNumber { get; private set; }
    public bool Owner2ShareDisplayName { get; private set; } = true;
    public bool Owner2SharePhoneNumber { get; private set; }
    public PetMatchStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public bool Involves(Guid userId) => userId == Owner1Id || userId == Owner2Id;

    public static PetMatch Create(Guid matchRequestId, Guid pet1Id, Guid pet2Id,
        Guid owner1Id, Guid owner2Id, bool owner1SharePhoneNumber,
        bool owner2SharePhoneNumber, DateTime utcNow) =>
        new(Guid.NewGuid(), matchRequestId, pet1Id, pet2Id, owner1Id, owner2Id,
            owner1SharePhoneNumber, owner2SharePhoneNumber, utcNow);
}
