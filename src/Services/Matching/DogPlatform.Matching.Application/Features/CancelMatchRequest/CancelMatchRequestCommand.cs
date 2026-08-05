using DogPlatform.Matching.Application.Features.Common;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.CancelMatchRequest;

public sealed record CancelMatchRequestCommand(Guid MatchRequestId) : IRequest<Result<MatchRequestResponse>>;
