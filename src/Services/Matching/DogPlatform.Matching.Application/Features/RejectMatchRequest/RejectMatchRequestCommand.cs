using DogPlatform.Matching.Application.Features.Common;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.RejectMatchRequest;

public sealed record RejectMatchRequestCommand(Guid MatchRequestId) : IRequest<Result<MatchRequestResponse>>;
