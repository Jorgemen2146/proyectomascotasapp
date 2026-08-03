using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.CalculateRelationship;

/// <summary>
/// Requests the estimated genealogical relationship between two pets.
/// </summary>
public sealed record CalculateRelationshipQuery(Guid PetId1, Guid PetId2, int? Depth)
    : IRequest<Result<RelationshipResponse>>;
