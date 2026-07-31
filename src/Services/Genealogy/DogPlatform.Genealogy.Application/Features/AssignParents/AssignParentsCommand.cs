using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.AssignParents;

/// <summary>
/// Assigns (or replaces) the father and/or mother of a pet.
/// OwnerId is resolved from the JWT — never accepted from the client.
/// </summary>
public sealed record AssignParentsCommand(
    Guid PetId,
    Guid? FatherId,
    Guid? MotherId) : IRequest<Result>;
