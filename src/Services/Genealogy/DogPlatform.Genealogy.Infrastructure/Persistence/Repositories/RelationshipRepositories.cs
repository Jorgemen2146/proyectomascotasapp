using DogPlatform.Genealogy.Domain.Relationships;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.Genealogy.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Genealogy.Infrastructure.Persistence.Repositories;

public sealed class PetRelationshipRepository(GenealogyDbContext context)
    : IPetRelationshipRepository
{
    public Task<PetRelationship?> GetByIdAsync(Guid relationshipId,
        CancellationToken cancellationToken = default) =>
        context.PetRelationships.FirstOrDefaultAsync(item => item.Id == relationshipId,
            cancellationToken);

    public Task<PetRelationship?> GetActiveForChildRoleAsync(Guid childPetId, ParentRole role,
        CancellationToken cancellationToken = default) =>
        context.PetRelationships.FirstOrDefaultAsync(item => item.ChildPetId == childPetId &&
            item.ParentRole == role && item.Status == PetRelationshipStatus.Active &&
            item.DeletedAtUtc == null, cancellationToken);

    public async Task<IReadOnlyList<PetRelationship>> GetActiveGraphAsync(
        CancellationToken cancellationToken = default) =>
        await context.PetRelationships.AsNoTracking()
            .Where(item => item.Status == PetRelationshipStatus.Active && item.DeletedAtUtc == null)
            .ToArrayAsync(cancellationToken);

    public async Task AddAsync(PetRelationship relationship,
        CancellationToken cancellationToken = default) =>
        await context.PetRelationships.AddAsync(relationship, cancellationToken);
}

public sealed class RelationshipInvitationRepository(GenealogyDbContext context)
    : IRelationshipInvitationRepository
{
    public Task<RelationshipInvitation?> GetByTokenHashAsync(string tokenHash,
        CancellationToken cancellationToken = default) =>
        context.RelationshipInvitations.FirstOrDefaultAsync(item => item.TokenHash == tokenHash,
            cancellationToken);

    public Task<RelationshipInvitation?> GetByIdAsync(Guid invitationId,
        CancellationToken cancellationToken = default) =>
        context.RelationshipInvitations.FirstOrDefaultAsync(item => item.Id == invitationId,
            cancellationToken);

    public Task<bool> HasPendingEquivalentAsync(Guid childPetId, ParentRole role,
        string targetEmail, CancellationToken cancellationToken = default) =>
        context.RelationshipInvitations.AnyAsync(item => item.ChildPetId == childPetId &&
            item.ParentRole == role && item.TargetEmail == targetEmail &&
            item.Status == RelationshipInvitationStatus.Pending, cancellationToken);

    public async Task<IReadOnlyList<RelationshipInvitation>> GetMineAsync(Guid userId,
        string email, RelationshipInvitationStatus? status,
        CancellationToken cancellationToken = default)
    {
        var normalized = RelationshipInvitation.NormalizeEmail(email);
        var query = context.RelationshipInvitations.Where(item =>
            item.RequesterUserId == userId || item.TargetUserId == userId ||
            item.TargetEmail == normalized);
        if (status.HasValue) query = query.Where(item => item.Status == status.Value);
        return await query.OrderByDescending(item => item.CreatedAtUtc).ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(RelationshipInvitation invitation,
        CancellationToken cancellationToken = default) =>
        await context.RelationshipInvitations.AddAsync(invitation, cancellationToken);
}
