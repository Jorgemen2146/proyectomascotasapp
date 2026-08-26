using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Repositories;
using MediatR;

namespace DogPlatform.Matching.Application.Features.Matches;

public sealed record GetMatchesQuery : IRequest<IReadOnlyList<PetMatchSummaryResponse>>;

public sealed class GetMatchesQueryHandler(
    IPetMatchRepository matches,
    IPetsMatchingClient pets,
    ICurrentUser currentUser) : IRequestHandler<GetMatchesQuery, IReadOnlyList<PetMatchSummaryResponse>>
{
    public async Task<IReadOnlyList<PetMatchSummaryResponse>> Handle(
        GetMatchesQuery request, CancellationToken cancellationToken)
    {
        var ownedMatches = await matches.GetByOwnerIdAsync(currentUser.UserId, cancellationToken);
        var petData = await pets.GetPetsByIdsAsync(ownedMatches
            .SelectMany(match => new[] { match.Pet1Id, match.Pet2Id }).Distinct().ToArray(), cancellationToken);
        var byId = petData.ToDictionary(pet => pet.PetId);
        return ownedMatches.Where(match => byId.ContainsKey(match.Pet1Id) && byId.ContainsKey(match.Pet2Id))
            .Select(match => new PetMatchSummaryResponse(match.Id,
                MatchMapping.PublicPet(byId[match.Pet1Id]), MatchMapping.PublicPet(byId[match.Pet2Id]),
                match.CreatedAtUtc)).ToList();
    }
}

internal static class MatchMapping
{
    internal static PublicMatchingPet PublicPet(PetMatchingDataResponse pet) => new(
        pet.PetId, pet.Name, pet.SpeciesName, pet.BreedName, pet.Sex,
        pet.AgeMonths, pet.MainPhotoUrl, pet.Color);
}
