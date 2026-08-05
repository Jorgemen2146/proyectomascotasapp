using DogPlatform.Matching.Application.Features.Common;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.CreateMatchRequest;

public sealed record CreateMatchRequestCommand(Guid PetId, Guid CandidatePetId, string? Message)
    : IRequest<Result<MatchRequestResponse>>;
