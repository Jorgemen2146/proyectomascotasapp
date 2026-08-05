using DogPlatform.Matching.Application.Common;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.GetFavorites;

public sealed record GetFavoritesQuery(Guid PetId, int PageNumber, int PageSize)
    : IRequest<Result<PagedResult<FavoriteCandidateResponse>>>;
