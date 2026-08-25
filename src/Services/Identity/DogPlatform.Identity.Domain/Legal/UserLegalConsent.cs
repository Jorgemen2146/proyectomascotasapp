using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Legal;

public sealed class UserLegalConsent : Entity<Guid>
{
    private UserLegalConsent(Guid id, Guid userId, Guid legalDocumentId, DateTime acceptedAtUtc)
        : base(id)
    {
        UserId = userId;
        LegalDocumentId = legalDocumentId;
        AcceptedAtUtc = acceptedAtUtc;
    }

    private UserLegalConsent() { }

    public Guid UserId { get; private set; }
    public Guid LegalDocumentId { get; private set; }
    public DateTime AcceptedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public static UserLegalConsent Accept(Guid id, Guid userId, Guid legalDocumentId,
        DateTime acceptedAtUtc) => new(id, userId, legalDocumentId, acceptedAtUtc);
}
