using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.GetDescendants;

/// <summary>
/// Returns the descendants of a pet (children, grandchildren, great-grandchildren, ...)
/// found by walking pets whose FatherId/MotherId points to the requested pet, generation
/// by generation.
/// </summary>
public sealed record GetDescendantsQuery(Guid PetId, int? Depth)
    : IRequest<Result<IReadOnlyList<DescendantResponse>>>;
