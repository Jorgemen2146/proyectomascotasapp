using System.Net.Mail;
using DogPlatform.Genealogy.Application.Security;
using DogPlatform.Genealogy.Domain.Errors;
using DogPlatform.Genealogy.Domain.Relationships;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DogPlatform.Genealogy.Application.Features.Relationships;

internal static class RelationshipRules
{
    public static bool TryParseRole(string value, out ParentRole role) =>
        Enum.TryParse(value, true, out role) && Enum.IsDefined(role);

    public static bool SexMatches(string sex, ParentRole role) => role switch
    {
        ParentRole.Father => sex.Equals("M", StringComparison.OrdinalIgnoreCase) ||
                             sex.Equals("Male", StringComparison.OrdinalIgnoreCase),
        ParentRole.Mother => sex.Equals("F", StringComparison.OrdinalIgnoreCase) ||
                             sex.Equals("Female", StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    public static Error? ValidateGraph(Guid childPetId, Guid parentPetId, ParentRole role,
        IReadOnlyCollection<PetRelationship> active)
    {
        if (childPetId == parentPetId)
            return GenealogyErrors.SelfRelationship;

        var assigned = active.FirstOrDefault(item => item.ChildPetId == childPetId &&
            item.ParentRole == role && item.IsActive);
        if (assigned is not null)
            return assigned.ParentPetId == parentPetId
                ? GenealogyErrors.RelationshipExists
                : GenealogyErrors.ParentAlreadyAssigned;

        if (active.Any(item => item.IsActive && item.ChildPetId == childPetId &&
                               item.ParentPetId == parentPetId))
            return GenealogyErrors.RelationshipExists;

        var childrenByParent = active.Where(item => item.IsActive)
            .GroupBy(item => item.ParentPetId)
            .ToDictionary(group => group.Key,
                group => group.Select(item => item.ChildPetId).Distinct().ToArray());
        var frontier = new Stack<Guid>();
        var visited = new HashSet<Guid>();
        frontier.Push(childPetId);
        while (frontier.Count > 0)
        {
            var current = frontier.Pop();
            if (!visited.Add(current))
                continue;
            if (current == parentPetId)
                return GenealogyErrors.CycleDetected;
            if (childrenByParent.TryGetValue(current, out var children))
                foreach (var child in children)
                    frontier.Push(child);
        }

        return null;
    }
}

public sealed class AddOwnParentCommandHandler(
    IPetRelationshipRepository relationships,
    IGenealogyUnitOfWork unitOfWork,
    IGenealogyPetService pets,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<AddOwnParentCommand, Result<RelationshipCreatedResponse>>
{
    public async Task<Result<RelationshipCreatedResponse>> Handle(
        AddOwnParentCommand request, CancellationToken cancellationToken)
    {
        if (!RelationshipRules.TryParseRole(request.ParentRole, out var role))
            return Result.Failure<RelationshipCreatedResponse>(GenealogyErrors.InvalidParentRole);

        var child = await pets.GetOwnedPetAsync(request.ChildPetId, currentUser.UserId, cancellationToken);
        var parent = await pets.GetOwnedPetAsync(request.ParentPetId, currentUser.UserId, cancellationToken);
        if (child is null || parent is null)
            return Result.Failure<RelationshipCreatedResponse>(GenealogyErrors.Forbidden);
        if (!RelationshipRules.SexMatches(parent.Sex, role))
            return Result.Failure<RelationshipCreatedResponse>(GenealogyErrors.ParentSexMismatch);

        var graph = await relationships.GetActiveGraphAsync(cancellationToken);
        var error = RelationshipRules.ValidateGraph(child.PetId, parent.PetId, role, graph);
        if (error is not null)
            return Result.Failure<RelationshipCreatedResponse>(error);

        var relationship = PetRelationship.CreateActive(child.PetId, parent.PetId, role,
            currentUser.UserId, timeProvider.GetUtcNow().UtcDateTime);
        await relationships.AddAsync(relationship, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new RelationshipCreatedResponse(relationship.Id, relationship.Status.ToString()));
    }
}

public sealed class DeleteRelationshipCommandHandler(
    IPetRelationshipRepository relationships,
    IGenealogyUnitOfWork unitOfWork,
    IGenealogyPetService pets,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<DeleteRelationshipCommand, Result>
{
    public async Task<Result> Handle(DeleteRelationshipCommand request, CancellationToken cancellationToken)
    {
        var relationship = await relationships.GetByIdAsync(request.RelationshipId, cancellationToken);
        if (relationship is null || !relationship.IsActive)
            return Result.Failure(GenealogyErrors.LineageNotFound);
        var child = await pets.GetOwnedPetAsync(relationship.ChildPetId, currentUser.UserId, cancellationToken);
        var parent = await pets.GetOwnedPetAsync(relationship.ParentPetId, currentUser.UserId, cancellationToken);
        if (child is null && parent is null)
            return Result.Failure(GenealogyErrors.Forbidden);
        relationship.SoftDelete(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class CreateInvitationCommandHandler(
    IRelationshipInvitationRepository invitations,
    IPetRelationshipRepository relationships,
    IGenealogyUnitOfWork unitOfWork,
    IGenealogyPetService pets,
    IInvitationTokenService tokens,
    IGenealogyInvitationEmailSender emailSender,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IOptions<GenealogyInvitationOptions> options,
    ILogger<CreateInvitationCommandHandler> logger)
    : IRequestHandler<CreateInvitationCommand, Result<InvitationCreatedResponse>>
{
    public async Task<Result<InvitationCreatedResponse>> Handle(
        CreateInvitationCommand request, CancellationToken cancellationToken)
    {
        if (!RelationshipRules.TryParseRole(request.ParentRole, out var role))
            return Result.Failure<InvitationCreatedResponse>(GenealogyErrors.InvalidParentRole);
        try { _ = new MailAddress(request.OwnerEmail); }
        catch { return Result.Failure<InvitationCreatedResponse>(GenealogyErrors.InvalidEmail); }
        var normalizedEmail = RelationshipInvitation.NormalizeEmail(request.OwnerEmail);
        if (normalizedEmail == RelationshipInvitation.NormalizeEmail(currentUser.Email))
            return Result.Failure<InvitationCreatedResponse>(GenealogyErrors.Forbidden);
        var child = await pets.GetOwnedPetAsync(request.ChildPetId, currentUser.UserId, cancellationToken);
        if (child is null)
            return Result.Failure<InvitationCreatedResponse>(GenealogyErrors.Forbidden);
        if (await relationships.GetActiveForChildRoleAsync(child.PetId, role, cancellationToken) is not null)
            return Result.Failure<InvitationCreatedResponse>(GenealogyErrors.ParentAlreadyAssigned);
        if (await invitations.HasPendingEquivalentAsync(child.PetId, role, normalizedEmail, cancellationToken))
            return Result.Failure<InvitationCreatedResponse>(GenealogyErrors.InvitationAlreadyPending);

        var rawToken = tokens.GenerateToken();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var invitation = RelationshipInvitation.Create(child.PetId, role, currentUser.UserId,
            currentUser.DisplayName, normalizedEmail, tokens.HashToken(rawToken),
            now.AddHours(options.Value.ExpirationHours), now);
        await invitations.AddAsync(invitation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        try { await emailSender.SendAsync(invitation, rawToken, cancellationToken); }
        catch (Exception exception) { logger.LogWarning(exception,
            "Genealogy invitation email delivery failed for InvitationId={InvitationId}.", invitation.Id); }
        return Result.Success(new InvitationCreatedResponse(invitation.Id,
            invitation.Status.ToString(), invitation.ExpiresAtUtc, rawToken));
    }
}

public sealed class GetInvitationQueryHandler(
    IRelationshipInvitationRepository invitations,
    IGenealogyUnitOfWork unitOfWork,
    IGenealogyPetService pets,
    IInvitationTokenService tokens,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<GetInvitationQuery, Result<InvitationContextResponse>>
{
    public async Task<Result<InvitationContextResponse>> Handle(
        GetInvitationQuery request, CancellationToken cancellationToken)
    {
        var invitation = await invitations.GetByTokenHashAsync(tokens.HashToken(request.Token), cancellationToken);
        if (invitation is null)
            return Result.Failure<InvitationContextResponse>(GenealogyErrors.InvitationInvalid);
        if (!invitation.IsForEmail(currentUser.Email))
            return Result.Failure<InvitationContextResponse>(GenealogyErrors.Forbidden);
        if (invitation.ExpireIfRequired(timeProvider.GetUtcNow().UtcDateTime))
            await unitOfWork.SaveChangesAsync(cancellationToken);
        var contexts = await pets.GetPetContextsAsync([invitation.ChildPetId], cancellationToken);
        if (!contexts.TryGetValue(invitation.ChildPetId, out var child))
            return Result.Failure<InvitationContextResponse>(GenealogyErrors.PetNotFound);
        return Result.Success(new InvitationContextResponse(invitation.Id,
            invitation.RequesterDisplayName, invitation.ChildPetId, child.Name,
            child.MainPhotoUrl, invitation.ParentRole.ToString(), invitation.ExpiresAtUtc,
            invitation.Status.ToString()));
    }
}

public sealed class AcceptInvitationCommandHandler(
    IRelationshipInvitationRepository invitations,
    IPetRelationshipRepository relationships,
    IGenealogyUnitOfWork unitOfWork,
    IGenealogyPetService pets,
    IInvitationTokenService tokens,
    IGenealogyNotificationPublisher notifications,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<AcceptInvitationCommandHandler> logger)
    : IRequestHandler<AcceptInvitationCommand, Result<RelationshipCreatedResponse>>
{
    public async Task<Result<RelationshipCreatedResponse>> Handle(
        AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await invitations.GetByTokenHashAsync(tokens.HashToken(request.Token), cancellationToken);
        if (invitation is null)
            return Result.Failure<RelationshipCreatedResponse>(GenealogyErrors.InvitationInvalid);
        if (!invitation.IsForEmail(currentUser.Email))
            return Result.Failure<RelationshipCreatedResponse>(GenealogyErrors.Forbidden);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (invitation.ExpireIfRequired(now))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<RelationshipCreatedResponse>(GenealogyErrors.InvitationExpired);
        }
        if (invitation.Status != RelationshipInvitationStatus.Pending)
            return Result.Failure<RelationshipCreatedResponse>(GenealogyErrors.InvitationAlreadyProcessed);
        var parent = await pets.GetOwnedPetAsync(request.PetId, currentUser.UserId, cancellationToken);
        if (parent is null)
            return Result.Failure<RelationshipCreatedResponse>(GenealogyErrors.Forbidden);
        if (!RelationshipRules.SexMatches(parent.Sex, invitation.ParentRole))
            return Result.Failure<RelationshipCreatedResponse>(GenealogyErrors.ParentSexMismatch);
        var contexts = await pets.GetPetContextsAsync([invitation.ChildPetId], cancellationToken);
        if (!contexts.TryGetValue(invitation.ChildPetId, out var child) ||
            child.OwnerUserId != invitation.RequesterUserId)
            return Result.Failure<RelationshipCreatedResponse>(GenealogyErrors.InvitationInvalid);
        var graph = await relationships.GetActiveGraphAsync(cancellationToken);
        var error = RelationshipRules.ValidateGraph(child.PetId, parent.PetId,
            invitation.ParentRole, graph);
        if (error is not null)
            return Result.Failure<RelationshipCreatedResponse>(error);
        var relationship = PetRelationship.CreateActive(child.PetId, parent.PetId,
            invitation.ParentRole, invitation.RequesterUserId, now);
        await relationships.AddAsync(relationship, cancellationToken);
        invitation.Accept(currentUser.UserId, parent.PetId, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishBestEffort("GenealogyRelationshipAccepted", invitation.RequesterUserId,
            invitation.Id, notifications, logger, cancellationToken);
        return Result.Success(new RelationshipCreatedResponse(relationship.Id,
            relationship.Status.ToString()));
    }

    internal static async Task PublishBestEffort(string eventType, Guid userId, Guid invitationId,
        IGenealogyNotificationPublisher publisher, ILogger logger, CancellationToken cancellationToken)
    {
        try { await publisher.PublishAsync(eventType, userId, invitationId, cancellationToken); }
        catch (Exception exception) { logger.LogWarning(exception,
            "Genealogy notification publishing failed for InvitationId={InvitationId}.", invitationId); }
    }
}

public sealed class RejectInvitationCommandHandler(
    IRelationshipInvitationRepository invitations, IGenealogyUnitOfWork unitOfWork,
    IInvitationTokenService tokens, IGenealogyNotificationPublisher notifications,
    ICurrentUser currentUser, TimeProvider timeProvider,
    ILogger<RejectInvitationCommandHandler> logger) : IRequestHandler<RejectInvitationCommand, Result>
{
    public async Task<Result> Handle(RejectInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await invitations.GetByTokenHashAsync(tokens.HashToken(request.Token), cancellationToken);
        if (invitation is null) return Result.Failure(GenealogyErrors.InvitationInvalid);
        if (!invitation.IsForEmail(currentUser.Email)) return Result.Failure(GenealogyErrors.Forbidden);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (invitation.ExpireIfRequired(now)) { await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(GenealogyErrors.InvitationExpired); }
        if (invitation.Status != RelationshipInvitationStatus.Pending)
            return Result.Failure(GenealogyErrors.InvitationAlreadyProcessed);
        invitation.Reject(currentUser.UserId, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await AcceptInvitationCommandHandler.PublishBestEffort("GenealogyRelationshipRejected",
            invitation.RequesterUserId, invitation.Id, notifications, logger, cancellationToken);
        return Result.Success();
    }
}

public sealed class CancelInvitationCommandHandler(
    IRelationshipInvitationRepository invitations, IGenealogyUnitOfWork unitOfWork,
    ICurrentUser currentUser, TimeProvider timeProvider) : IRequestHandler<CancelInvitationCommand, Result>
{
    public async Task<Result> Handle(CancelInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await invitations.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation is null) return Result.Failure(GenealogyErrors.InvitationInvalid);
        if (invitation.RequesterUserId != currentUser.UserId) return Result.Failure(GenealogyErrors.Forbidden);
        if (invitation.Status != RelationshipInvitationStatus.Pending)
            return Result.Failure(GenealogyErrors.InvitationAlreadyProcessed);
        invitation.Cancel(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetMyInvitationsQueryHandler(
    IRelationshipInvitationRepository invitations, IGenealogyPetService pets,
    ICurrentUser currentUser, TimeProvider timeProvider, IGenealogyUnitOfWork unitOfWork)
    : IRequestHandler<GetMyInvitationsQuery, Result<IReadOnlyList<InvitationListItemResponse>>>
{
    public async Task<Result<IReadOnlyList<InvitationListItemResponse>>> Handle(
        GetMyInvitationsQuery request, CancellationToken cancellationToken)
    {
        RelationshipInvitationStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<RelationshipInvitationStatus>(request.Status, true, out var parsedStatus))
                return Result.Failure<IReadOnlyList<InvitationListItemResponse>>(GenealogyErrors.InvitationInvalid);
            status = parsedStatus;
        }
        var items = await invitations.GetMineAsync(currentUser.UserId, currentUser.Email, null, cancellationToken);
        var changed = false;
        foreach (var item in items) changed |= item.ExpireIfRequired(timeProvider.GetUtcNow().UtcDateTime);
        if (changed) await unitOfWork.SaveChangesAsync(cancellationToken);
        var contexts = await pets.GetPetContextsAsync(items.Select(item => item.ChildPetId).Distinct().ToArray(), cancellationToken);
        var direction = request.Direction?.Trim().ToLowerInvariant();
        var response = items.Where(item => !status.HasValue || item.Status == status.Value)
            .Where(item => direction switch
            {
                "incoming" => item.RequesterUserId != currentUser.UserId,
                "outgoing" => item.RequesterUserId == currentUser.UserId,
                _ => true
            })
            .Select(item => new InvitationListItemResponse(item.Id, item.ChildPetId,
                contexts.TryGetValue(item.ChildPetId, out var child) ? child.Name : "Unknown",
                item.ParentRole.ToString(), item.RequesterUserId == currentUser.UserId ? "Outgoing" : "Incoming",
                item.Status.ToString(), item.ExpiresAtUtc, item.CreatedAtUtc)).ToArray();
        return Result.Success<IReadOnlyList<InvitationListItemResponse>>(response);
    }
}

public sealed class GetRelationshipTreeQueryHandler(
    IPetRelationshipRepository relationships, IGenealogyPetService pets,
    ICurrentUser currentUser) : IRequestHandler<GetRelationshipTreeQuery, Result<RelationshipTreeResponse>>
{
    public async Task<Result<RelationshipTreeResponse>> Handle(
        GetRelationshipTreeQuery request, CancellationToken cancellationToken)
    {
        if (request.Generations is < 1 or > 5)
            return Result.Failure<RelationshipTreeResponse>(GenealogyErrors.InvalidGenerations);
        var graph = await relationships.GetActiveGraphAsync(cancellationToken);
        GenealogyPetContext? root;
        IReadOnlyDictionary<Guid, GenealogyPetContext> petContexts;
        try
        {
            root = await pets.GetOwnedPetAsync(request.PetId, currentUser.UserId, cancellationToken);
            if (root is null)
                return Result.Failure<RelationshipTreeResponse>(GenealogyErrors.Forbidden);
            var relevantIds = CollectRelevantIds(root.PetId, request.Generations, graph);
            petContexts = await pets.GetPetContextsAsync(relevantIds, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result.Failure<RelationshipTreeResponse>(GenealogyErrors.PetsServiceUnavailable);
        }

        var contexts = new Dictionary<Guid, GenealogyPetContext>(petContexts);
        contexts[root.PetId] = root;
        var parents = BuildParents(root.PetId, 1, request.Generations, graph, contexts);
        var children = graph.Where(item => item.IsActive && item.ParentPetId == root.PetId)
            .Select(item => new GenealogyChildNode(item.Id,
                ToNode(contexts.GetValueOrDefault(item.ChildPetId), item.ChildPetId, "Unknown")))
            .ToArray();
        return Result.Success(new RelationshipTreeResponse(ToNode(root, root.PetId, root.Sex), parents, children));
    }

    private static Guid[] CollectRelevantIds(Guid rootId, int generations,
        IReadOnlyCollection<PetRelationship> graph)
    {
        var ids = new HashSet<Guid> { rootId };
        var frontier = new HashSet<Guid> { rootId };
        for (var level = 0; level < generations; level++)
        {
            var next = graph.Where(item => item.IsActive && frontier.Contains(item.ChildPetId))
                .Select(item => item.ParentPetId).ToHashSet();
            ids.UnionWith(next); frontier = next;
        }
        ids.UnionWith(graph.Where(item => item.IsActive && item.ParentPetId == rootId)
            .Select(item => item.ChildPetId));
        return [.. ids];
    }

    private static IReadOnlyList<GenealogyParentNode> BuildParents(Guid childId, int level,
        int max, IReadOnlyCollection<PetRelationship> graph,
        IReadOnlyDictionary<Guid, GenealogyPetContext> contexts)
    {
        if (level > max) return [];
        return graph.Where(item => item.IsActive && item.ChildPetId == childId)
            .OrderBy(item => item.ParentRole)
            .Select(item => new GenealogyParentNode(item.Id, item.ParentRole.ToString(),
                ToNode(contexts.GetValueOrDefault(item.ParentPetId), item.ParentPetId,
                    item.ParentRole == ParentRole.Father ? "M" : "F"),
                BuildParents(item.ParentPetId, level + 1, max, graph, contexts)))
            .ToArray();
    }

    private static GenealogyPetNode ToNode(GenealogyPetContext? pet, Guid petId, string fallbackSex) =>
        new(petId, pet?.Name ?? "Unknown", pet?.Sex is null or "Unknown" ? fallbackSex : pet.Sex,
            pet?.SpeciesId ?? 0, pet?.BreedName, pet?.MainPhotoUrl, pet?.BirthDate);
}
