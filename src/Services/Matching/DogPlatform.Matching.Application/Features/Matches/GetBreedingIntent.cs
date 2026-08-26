using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.Matches;

public sealed record GetBreedingIntentQuery(Guid MatchId)
    : IRequest<Result<BreedingIntentResponse>>;

public sealed class GetBreedingIntentQueryHandler(
    IPetMatchRepository matches,
    IBreedingIntentRepository intents,
    ICurrentUser currentUser)
    : IRequestHandler<GetBreedingIntentQuery, Result<BreedingIntentResponse>>
{
    public async Task<Result<BreedingIntentResponse>> Handle(
        GetBreedingIntentQuery request, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdAsync(request.MatchId, cancellationToken);
        if (match is null)
            return Result.Failure<BreedingIntentResponse>(MatchingErrors.MatchNotFound);
        if (!match.Involves(currentUser.UserId))
            return Result.Failure<BreedingIntentResponse>(MatchingErrors.Forbidden);

        var intent = await intents.GetLatestByMatchIdAsync(match.Id, cancellationToken);
        if (intent is null)
            return Result.Failure<BreedingIntentResponse>(MatchingErrors.BreedingIntentNotFound);

        return Result.Success(BreedingIntentMapping.Full(intent, currentUser.UserId));
    }
}

internal static class BreedingIntentMapping
{
    internal static BreedingIntentResponse Full(
        Domain.Aggregates.BreedingIntent.BreedingIntent intent, Guid currentUserId) =>
        new(intent.Id, intent.MatchId, intent.Status.ToString(), intent.Notes,
            intent.ExpectedDateUtc, intent.CreatedAtUtc, intent.AcceptedAtUtc,
            intent.CancelledAtUtc, intent.ProposerOwnerId == currentUserId);

    internal static BreedingIntentSummaryResponse Summary(
        Domain.Aggregates.BreedingIntent.BreedingIntent intent, Guid currentUserId) =>
        new(intent.Id, intent.Status.ToString(), intent.Notes, intent.ExpectedDateUtc,
            intent.CreatedAtUtc, intent.ProposerOwnerId == currentUserId);
}
