using DogPlatform.Matching.Application.Common;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.SearchCandidates;

public sealed record SearchCandidatesQuery(
    Guid PetId,
    int PageNumber,
    int PageSize,
    int? BreedId,
    int? MinimumAgeMonths,
    int? MaximumAgeMonths,
    int? MinimumScore,
    string SortBy,
    string SortDirection,
    bool FavoritesOnly) : IRequest<Result<PagedResult<CandidateSummaryResponse>>>;
