using DogPlatform.Matching.Application.Features.Common;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.AcceptMatchRequest;

public sealed record AcceptMatchRequestCommand(Guid MatchRequestId, bool SharePhoneNumber = false)
    : IRequest<Result<MatchRequestResponse>>;
