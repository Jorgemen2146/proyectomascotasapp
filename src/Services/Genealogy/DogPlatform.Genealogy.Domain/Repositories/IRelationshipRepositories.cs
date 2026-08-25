using DogPlatform.Genealogy.Domain.Relationships;

namespace DogPlatform.Genealogy.Domain.Repositories;

public interface IPetRelationshipRepository
{
    Task<PetRelationship?> GetByIdAsync(Guid relationshipId,
        CancellationToken cancellationToken = default);
    Task<PetRelationship?> GetActiveForChildRoleAsync(Guid childPetId, ParentRole role,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PetRelationship>> GetActiveGraphAsync(
        CancellationToken cancellationToken = default);
    Task AddAsync(PetRelationship relationship, CancellationToken cancellationToken = default);
}

public interface IRelationshipInvitationRepository
{
    Task<RelationshipInvitation?> GetByTokenHashAsync(string tokenHash,
        CancellationToken cancellationToken = default);
    Task<RelationshipInvitation?> GetByIdAsync(Guid invitationId,
        CancellationToken cancellationToken = default);
    Task<bool> HasPendingEquivalentAsync(Guid childPetId, ParentRole role, string targetEmail,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RelationshipInvitation>> GetMineAsync(Guid userId, string email,
        RelationshipInvitationStatus? status, CancellationToken cancellationToken = default);
    Task AddAsync(RelationshipInvitation invitation, CancellationToken cancellationToken = default);
}
