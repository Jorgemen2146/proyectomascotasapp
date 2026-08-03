using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.GetSiblings;

/// <summary>
/// Calculates siblings of a pet from shared FatherId/MotherId. There is no siblings
/// table; the relationship is derived on demand.
/// </summary>
public sealed record GetSiblingsQuery(Guid PetId) : IRequest<Result<IReadOnlyList<SiblingResponse>>>;
