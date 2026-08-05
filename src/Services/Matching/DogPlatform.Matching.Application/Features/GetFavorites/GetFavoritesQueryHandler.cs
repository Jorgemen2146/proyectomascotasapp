using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Common;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.GetFavorites;

public sealed class GetFavoritesQueryHandler
    : IRequestHandler<GetFavoritesQuery, Result<PagedResult<FavoriteCandidateResponse>>>
{
    private readonly IFavoriteCandidateRepository _favoriteRepository;
    private readonly IMatchingProfileRepository _profileRepository;
    private readonly IPetsMatchingClient _petsClient;
    private readonly ICurrentUser _currentUser;

    public GetFavoritesQueryHandler(
        IFavoriteCandidateRepository favoriteRepository,
        IMatchingProfileRepository profileRepository,
        IPetsMatchingClient petsClient,
        ICurrentUser currentUser)
    {
        _favoriteRepository = favoriteRepository;
        _profileRepository = profileRepository;
        _petsClient = petsClient;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<FavoriteCandidateResponse>>> Handle(
        GetFavoritesQuery request, CancellationToken cancellationToken)
    {
        var sourcePet = await _petsClient.GetPetForMatchingAsync(request.PetId, cancellationToken);
        if (sourcePet is null || sourcePet.IsDeleted)
            return Result.Failure<PagedResult<FavoriteCandidateResponse>>(MatchingErrors.PetNotFound);

        if (sourcePet.OwnerId != _currentUser.UserId)
            return Result.Failure<PagedResult<FavoriteCandidateResponse>>(MatchingErrors.Unauthorized);

        var (favorites, totalItems) = await _favoriteRepository.GetPagedAsync(
            request.PetId, request.PageNumber, request.PageSize, cancellationToken);

        if (favorites.Count == 0)
            return Result.Success(PagedResult<FavoriteCandidateResponse>.Create(
                [], totalItems, request.PageNumber, request.PageSize));

        var candidateIds = favorites.Select(f => f.CandidatePetId).ToList();
        var candidates = await _petsClient.GetPetsByIdsAsync(candidateIds, cancellationToken);
        var candidateLookup = candidates.ToDictionary(c => c.PetId);

        var items = new List<FavoriteCandidateResponse>();
        foreach (var favorite in favorites)
        {
            if (!candidateLookup.TryGetValue(favorite.CandidatePetId, out var candidate))
                continue;

            var candidateProfile = await _profileRepository.GetActiveByPetIdAsync(
                favorite.CandidatePetId, cancellationToken);

            items.Add(new FavoriteCandidateResponse(
                candidate.PetId,
                candidate.Name,
                candidate.BreedId,
                candidate.BreedName,
                candidate.Sex,
                candidate.AgeMonths,
                candidate.MainPhotoUrl,
                candidateProfile is not null && !candidate.IsDeleted && candidate.IsActive,
                favorite.CreatedAt));
        }

        return Result.Success(
            PagedResult<FavoriteCandidateResponse>.Create(items, totalItems, request.PageNumber, request.PageSize));
    }
}
