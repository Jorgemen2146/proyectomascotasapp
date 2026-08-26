using DogPlatform.Matching.Application.Clients.Identity;
using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.Matches;

public sealed record GetMatchDetailQuery(Guid MatchId) : IRequest<Result<PetMatchDetailResponse>>;

public sealed class GetMatchDetailQueryHandler(
    IPetMatchRepository matches,
    IBreedingIntentRepository intents,
    IPetsMatchingClient pets,
    IIdentityMatchingClient identity,
    ICurrentUser currentUser) : IRequestHandler<GetMatchDetailQuery, Result<PetMatchDetailResponse>>
{
    public async Task<Result<PetMatchDetailResponse>> Handle(
        GetMatchDetailQuery request, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdAsync(request.MatchId, cancellationToken);
        if (match is null) return Result.Failure<PetMatchDetailResponse>(MatchingErrors.MatchNotFound);
        if (!match.Involves(currentUser.UserId))
            return Result.Failure<PetMatchDetailResponse>(MatchingErrors.Forbidden);
        if (match.Status != Domain.Enums.PetMatchStatus.Active)
            return Result.Failure<PetMatchDetailResponse>(MatchingErrors.MatchNotAccepted);

        var petData = await pets.GetPetsByIdsAsync([match.Pet1Id, match.Pet2Id], cancellationToken);
        var byId = petData.ToDictionary(pet => pet.PetId);
        if (!byId.TryGetValue(match.Pet1Id, out var pet1) || !byId.TryGetValue(match.Pet2Id, out var pet2))
            return Result.Failure<PetMatchDetailResponse>(MatchingErrors.PetNotFound);

        var owner1 = await identity.GetMatchingContactAsync(match.Owner1Id, cancellationToken);
        var owner2 = await identity.GetMatchingContactAsync(match.Owner2Id, cancellationToken);
        if (owner1 is null || owner2 is null)
            return Result.Failure<PetMatchDetailResponse>(MatchingErrors.ContactNotShared);

        var breedingIntent = await intents.GetLatestByMatchIdAsync(match.Id, cancellationToken);

        return Result.Success(new PetMatchDetailResponse(
            match.Id,
            match.Status.ToString(),
            MatchMapping.PublicPet(pet1),
            MatchMapping.PublicPet(pet2),
            new SharedOwnerContact(match.Owner1ShareDisplayName ? owner1.DisplayName : null,
                match.Owner1SharePhoneNumber ? owner1.PhoneNumber : null),
            new SharedOwnerContact(match.Owner2ShareDisplayName ? owner2.DisplayName : null,
                match.Owner2SharePhoneNumber ? owner2.PhoneNumber : null),
            match.CreatedAtUtc,
            breedingIntent is null
                ? null
                : BreedingIntentMapping.Summary(breedingIntent, currentUser.UserId)));
    }
}
