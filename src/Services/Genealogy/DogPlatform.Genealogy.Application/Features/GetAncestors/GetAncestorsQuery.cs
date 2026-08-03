using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.GetAncestors;

/// <summary>
/// Returns a flattened list of ancestors (father, mother, grandparents, ...) with the
/// lineage path(s) leading to each one.
/// </summary>
public sealed record GetAncestorsQuery(Guid PetId, int? Depth)
    : IRequest<Result<IReadOnlyList<AncestorResponse>>>;
