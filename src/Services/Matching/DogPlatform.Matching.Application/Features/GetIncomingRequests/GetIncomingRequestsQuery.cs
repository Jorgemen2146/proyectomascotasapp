using DogPlatform.Matching.Application.Common;
using DogPlatform.Matching.Application.Features.Common;
using DogPlatform.Matching.Domain.Enums;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.GetIncomingRequests;

public sealed record GetIncomingRequestsQuery(int PageNumber, int PageSize, MatchRequestStatus? Status)
    : IRequest<Result<PagedResult<MatchRequestResponse>>>;
