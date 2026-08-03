using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.GetAncestorTree;

/// <summary>
/// Builds the ancestor tree (father/mother, grandparents, great-grandparents, ...) of a pet.
/// Depth is optional; defaults to <see cref="Options.GenealogyOptions.DefaultTreeDepth"/> and is
/// capped at <see cref="Options.GenealogyOptions.MaximumTreeDepth"/>.
/// </summary>
public sealed record GetAncestorTreeQuery(Guid PetId, int? Depth) : IRequest<Result<GenealogyTreeResponse>>;
