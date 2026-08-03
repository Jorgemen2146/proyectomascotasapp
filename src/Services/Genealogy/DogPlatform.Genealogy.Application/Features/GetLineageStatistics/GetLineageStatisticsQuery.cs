using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.GetLineageStatistics;

/// <summary>
/// Requests pedigree statistics (completeness, generation distribution, repeated
/// ancestors, and estimated inbreeding coefficient) for a pet.
/// </summary>
public sealed record GetLineageStatisticsQuery(Guid PetId, int? Depth)
    : IRequest<Result<LineageStatisticsResponse>>;
