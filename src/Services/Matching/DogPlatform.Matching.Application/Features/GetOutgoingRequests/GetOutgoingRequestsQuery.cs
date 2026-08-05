using DogPlatform.Matching.Application.Common;
using DogPlatform.Matching.Application.Features.Common;
using DogPlatform.Matching.Domain.Enums;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.GetOutgoingRequests;

public sealed record GetOutgoingRequestsQuery(int PageNumber, int PageSize, MatchRequestStatus? Status)
    : IRequest<Result<PagedResult<MatchRequestResponse>>>;
