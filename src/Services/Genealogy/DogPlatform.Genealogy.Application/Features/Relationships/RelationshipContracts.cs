using DogPlatform.Genealogy.Domain.Relationships;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.Relationships;

public sealed record GenealogyPetContext(
    Guid PetId,
    Guid OwnerUserId,
    string Name,
    int SpeciesId,
    string? BreedName,
    string Sex,
    DateTime? BirthDate,
    string? MainPhotoUrl);

public interface IGenealogyPetService
{
    Task<GenealogyPetContext?> GetOwnedPetAsync(Guid petId, Guid ownerUserId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, GenealogyPetContext>> GetPetContextsAsync(
        IReadOnlyCollection<Guid> petIds, CancellationToken cancellationToken = default);
}

public interface IInvitationTokenService
{
    string GenerateToken();
    string HashToken(string token);
}

public interface IGenealogyInvitationEmailSender
{
    Task SendAsync(RelationshipInvitation invitation, string token,
        CancellationToken cancellationToken = default);
}

public interface IGenealogyNotificationPublisher
{
    Task PublishAsync(string eventType, Guid userId, Guid invitationId,
        CancellationToken cancellationToken = default);
}

public sealed class GenealogyInvitationOptions
{
    public const string SectionName = "GenealogyInvitations";
    public int ExpirationHours { get; set; } = 72;
}

public sealed record RelationshipCreatedResponse(Guid RelationshipId, string Status);
public sealed record InvitationCreatedResponse(Guid InvitationId, string Status,
    DateTime ExpiresAtUtc, string InvitationToken);
public sealed record InvitationContextResponse(Guid InvitationId, string RequesterDisplayName,
    Guid ChildPetId, string ChildPetName, string? ChildMainPhotoUrl, string ParentRole,
    DateTime ExpiresAtUtc, string Status);
public sealed record InvitationListItemResponse(Guid InvitationId, Guid ChildPetId,
    string ChildPetName, string ParentRole, string Direction, string Status,
    DateTime ExpiresAtUtc, DateTime CreatedAtUtc);

public sealed record GenealogyPetNode(Guid PetId, string Name, string Sex, int SpeciesId,
    string? BreedName, string? MainPhotoUrl, DateTime? BirthDate);
public sealed record GenealogyParentNode(Guid RelationshipId, string Role,
    GenealogyPetNode Pet, IReadOnlyList<GenealogyParentNode> Parents);
public sealed record GenealogyChildNode(Guid RelationshipId, GenealogyPetNode Pet);
public sealed record RelationshipTreeResponse(GenealogyPetNode Pet,
    IReadOnlyList<GenealogyParentNode> Parents, IReadOnlyList<GenealogyChildNode> Children);

public sealed record AddOwnParentCommand(Guid ChildPetId, Guid ParentPetId, string ParentRole)
    : IRequest<Result<RelationshipCreatedResponse>>;
public sealed record DeleteRelationshipCommand(Guid RelationshipId) : IRequest<Result>;
public sealed record CreateInvitationCommand(Guid ChildPetId, string ParentRole, string OwnerEmail)
    : IRequest<Result<InvitationCreatedResponse>>;
public sealed record GetInvitationQuery(string Token) : IRequest<Result<InvitationContextResponse>>;
public sealed record AcceptInvitationCommand(string Token, Guid PetId)
    : IRequest<Result<RelationshipCreatedResponse>>;
public sealed record RejectInvitationCommand(string Token) : IRequest<Result>;
public sealed record CancelInvitationCommand(Guid InvitationId) : IRequest<Result>;
public sealed record GetMyInvitationsQuery(string? Direction, string? Status)
    : IRequest<Result<IReadOnlyList<InvitationListItemResponse>>>;
public sealed record GetRelationshipTreeQuery(Guid PetId, int Generations)
    : IRequest<Result<RelationshipTreeResponse>>;
