using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Genealogy.Domain.Relationships;

public enum ParentRole
{
    Father = 1,
    Mother = 2
}

public enum PetRelationshipStatus
{
    Pending = 1,
    Active = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum RelationshipInvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Cancelled = 4,
    Expired = 5
}

public sealed class PetRelationship : Entity<Guid>
{
    private PetRelationship() { }

    private PetRelationship(Guid id, Guid childPetId, Guid parentPetId,
        ParentRole parentRole, PetRelationshipStatus status,
        Guid createdByUserId, DateTime createdAtUtc)
        : base(id)
    {
        ChildPetId = childPetId;
        ParentPetId = parentPetId;
        ParentRole = parentRole;
        Status = status;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        ActivatedAtUtc = status == PetRelationshipStatus.Active ? createdAtUtc : null;
    }

    public Guid ChildPetId { get; private set; }
    public Guid ParentPetId { get; private set; }
    public ParentRole ParentRole { get; private set; }
    public PetRelationshipStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ActivatedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public bool IsActive => Status == PetRelationshipStatus.Active && DeletedAtUtc is null;

    public static PetRelationship CreateActive(Guid childPetId, Guid parentPetId,
        ParentRole parentRole, Guid createdByUserId, DateTime utcNow) =>
        new(Guid.NewGuid(), childPetId, parentPetId, parentRole,
            PetRelationshipStatus.Active, createdByUserId, utcNow);

    public static PetRelationship CreatePending(Guid childPetId, Guid parentPetId,
        ParentRole parentRole, Guid createdByUserId, DateTime utcNow) =>
        new(Guid.NewGuid(), childPetId, parentPetId, parentRole,
            PetRelationshipStatus.Pending, createdByUserId, utcNow);

    public void SoftDelete(DateTime utcNow)
    {
        DeletedAtUtc = utcNow;
        Status = PetRelationshipStatus.Cancelled;
    }
}

public sealed class RelationshipInvitation : Entity<Guid>
{
    private RelationshipInvitation() { }

    private RelationshipInvitation(Guid id, Guid childPetId, ParentRole parentRole,
        Guid requesterUserId, string requesterDisplayName, string targetEmail,
        string tokenHash, DateTime expiresAtUtc, DateTime createdAtUtc)
        : base(id)
    {
        ChildPetId = childPetId;
        ParentRole = parentRole;
        RequesterUserId = requesterUserId;
        RequesterDisplayName = requesterDisplayName;
        TargetEmail = NormalizeEmail(targetEmail);
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        Status = RelationshipInvitationStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ChildPetId { get; private set; }
    public ParentRole ParentRole { get; private set; }
    public Guid RequesterUserId { get; private set; }
    public string RequesterDisplayName { get; private set; } = string.Empty;
    public Guid? TargetUserId { get; private set; }
    public string TargetEmail { get; private set; } = string.Empty;
    public Guid? SelectedTargetPetId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public RelationshipInvitationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public static RelationshipInvitation Create(Guid childPetId, ParentRole parentRole,
        Guid requesterUserId, string requesterDisplayName, string targetEmail,
        string tokenHash, DateTime expiresAtUtc, DateTime utcNow) =>
        new(Guid.NewGuid(), childPetId, parentRole, requesterUserId,
            requesterDisplayName, targetEmail, tokenHash, expiresAtUtc, utcNow);

    public bool IsForEmail(string email) =>
        string.Equals(TargetEmail, NormalizeEmail(email), StringComparison.OrdinalIgnoreCase);

    public bool ExpireIfRequired(DateTime utcNow)
    {
        if (Status != RelationshipInvitationStatus.Pending || utcNow < ExpiresAtUtc)
            return false;
        Status = RelationshipInvitationStatus.Expired;
        return true;
    }

    public void Accept(Guid targetUserId, Guid selectedTargetPetId, DateTime utcNow)
    {
        TargetUserId = targetUserId;
        SelectedTargetPetId = selectedTargetPetId;
        Status = RelationshipInvitationStatus.Accepted;
        AcceptedAtUtc = utcNow;
    }

    public void Reject(Guid targetUserId, DateTime utcNow)
    {
        TargetUserId = targetUserId;
        Status = RelationshipInvitationStatus.Rejected;
        RejectedAtUtc = utcNow;
    }

    public void Cancel(DateTime utcNow)
    {
        Status = RelationshipInvitationStatus.Cancelled;
        CancelledAtUtc = utcNow;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
