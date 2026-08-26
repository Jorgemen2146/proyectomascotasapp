using DogPlatform.Matching.Application.Clients.Notifications;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.Matches;

public sealed record ProposeBreedingIntentCommand(Guid MatchId, string? Notes,
    DateTime? ExpectedDateUtc) : IRequest<Result<BreedingIntentResponse>>;
public sealed record AcceptBreedingIntentCommand(Guid BreedingIntentId)
    : IRequest<Result<BreedingIntentResponse>>;
public sealed record CancelBreedingIntentCommand(Guid BreedingIntentId)
    : IRequest<Result<BreedingIntentResponse>>;

public sealed class ProposeBreedingIntentCommandHandler(
    IPetMatchRepository matches, IBreedingIntentRepository intents,
    IMatchingUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider timeProvider,
    IMatchingNotificationClient notifications)
    : IRequestHandler<ProposeBreedingIntentCommand, Result<BreedingIntentResponse>>
{
    public async Task<Result<BreedingIntentResponse>> Handle(
        ProposeBreedingIntentCommand request, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdAsync(request.MatchId, cancellationToken);
        if (match is null) return Result.Failure<BreedingIntentResponse>(MatchingErrors.MatchNotFound);
        if (!match.Involves(currentUser.UserId))
            return Result.Failure<BreedingIntentResponse>(MatchingErrors.Forbidden);
        if (match.Status != Domain.Enums.PetMatchStatus.Active)
            return Result.Failure<BreedingIntentResponse>(MatchingErrors.MatchNotAccepted);
        if (await intents.HasOpenIntentAsync(match.Id, cancellationToken))
            return Result.Failure<BreedingIntentResponse>(MatchingErrors.BreedingIntentExists);

        var creation = Domain.Aggregates.BreedingIntent.BreedingIntent.Create(match.Id,
            currentUser.UserId, request.Notes, request.ExpectedDateUtc,
            timeProvider.GetUtcNow().UtcDateTime);
        if (creation.IsFailure) return Result.Failure<BreedingIntentResponse>(creation.Error);
        intents.Add(creation.Value);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (BreedingIntentConflictException)
        {
            return Result.Failure<BreedingIntentResponse>(MatchingErrors.BreedingIntentExists);
        }

        await notifications.SendAsync(new MatchingNotification(
            OtherOwner(match.Owner1Id, match.Owner2Id, currentUser.UserId),
            "MatchingBreedingIntentProposed",
            "Nueva propuesta de camada",
            "Recibiste una propuesta de posible camada.",
            Metadata: new MatchingNotificationMetadata(match.Id, creation.Value.Id)),
            cancellationToken);

        return Result.Success(BreedingIntentMapping.Full(creation.Value, currentUser.UserId));
    }

    internal static Guid OtherOwner(Guid owner1Id, Guid owner2Id, Guid currentUserId) =>
        currentUserId == owner1Id ? owner2Id : owner1Id;
}

public sealed class AcceptBreedingIntentCommandHandler(
    IPetMatchRepository matches, IBreedingIntentRepository intents,
    IMatchingUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider timeProvider,
    IMatchingNotificationClient notifications)
    : IRequestHandler<AcceptBreedingIntentCommand, Result<BreedingIntentResponse>>
{
    public async Task<Result<BreedingIntentResponse>> Handle(AcceptBreedingIntentCommand request,
        CancellationToken cancellationToken)
    {
        var intent = await intents.GetByIdAsync(request.BreedingIntentId, cancellationToken);
        if (intent is null) return Result.Failure<BreedingIntentResponse>(MatchingErrors.BreedingIntentNotFound);
        var match = await matches.GetByIdAsync(intent.MatchId, cancellationToken);
        if (match is null) return Result.Failure<BreedingIntentResponse>(MatchingErrors.MatchNotFound);
        if (!match.Involves(currentUser.UserId))
            return Result.Failure<BreedingIntentResponse>(MatchingErrors.Forbidden);
        if (match.Status != Domain.Enums.PetMatchStatus.Active)
            return Result.Failure<BreedingIntentResponse>(MatchingErrors.MatchNotAccepted);
        var result = intent.Accept(currentUser.UserId, match.Owner1Id, match.Owner2Id,
            timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure) return Result.Failure<BreedingIntentResponse>(result.Error);
        intents.Update(intent);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notifications.SendAsync(new MatchingNotification(
            intent.ProposerOwnerId,
            "MatchingBreedingIntentAccepted",
            "Propuesta de camada aceptada",
            "Tu propuesta de posible camada fue aceptada.",
            Metadata: new MatchingNotificationMetadata(match.Id, intent.Id)),
            cancellationToken);
        return Result.Success(BreedingIntentMapping.Full(intent, currentUser.UserId));
    }
}

public sealed class CancelBreedingIntentCommandHandler(
    IPetMatchRepository matches, IBreedingIntentRepository intents,
    IMatchingUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider timeProvider,
    IMatchingNotificationClient notifications)
    : IRequestHandler<CancelBreedingIntentCommand, Result<BreedingIntentResponse>>
{
    public async Task<Result<BreedingIntentResponse>> Handle(CancelBreedingIntentCommand request,
        CancellationToken cancellationToken)
    {
        var intent = await intents.GetByIdAsync(request.BreedingIntentId, cancellationToken);
        if (intent is null) return Result.Failure<BreedingIntentResponse>(MatchingErrors.BreedingIntentNotFound);
        var match = await matches.GetByIdAsync(intent.MatchId, cancellationToken);
        if (match is null) return Result.Failure<BreedingIntentResponse>(MatchingErrors.MatchNotFound);
        if (!match.Involves(currentUser.UserId))
            return Result.Failure<BreedingIntentResponse>(MatchingErrors.Forbidden);
        if (match.Status != Domain.Enums.PetMatchStatus.Active)
            return Result.Failure<BreedingIntentResponse>(MatchingErrors.MatchNotAccepted);
        var result = intent.Cancel(currentUser.UserId, match.Owner1Id, match.Owner2Id,
            timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure) return Result.Failure<BreedingIntentResponse>(result.Error);
        intents.Update(intent);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notifications.SendAsync(new MatchingNotification(
            ProposeBreedingIntentCommandHandler.OtherOwner(
                match.Owner1Id, match.Owner2Id, currentUser.UserId),
            "MatchingBreedingIntentCancelled",
            "Propuesta de camada cancelada",
            "La propuesta de posible camada fue cancelada.",
            Metadata: new MatchingNotificationMetadata(match.Id, intent.Id)),
            cancellationToken);
        return Result.Success(BreedingIntentMapping.Full(intent, currentUser.UserId));
    }
}
