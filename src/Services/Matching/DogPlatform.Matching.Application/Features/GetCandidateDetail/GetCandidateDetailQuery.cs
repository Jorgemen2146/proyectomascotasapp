using DogPlatform.Matching.Application.Features.SearchCandidates;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.GetCandidateDetail;

public sealed record GetCandidateDetailQuery(Guid PetId, Guid CandidatePetId)
    : IRequest<Result<CandidateSummaryResponse>>;
