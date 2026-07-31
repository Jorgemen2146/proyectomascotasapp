using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.GetParents;

/// <summary>Returns the direct parents (father and/or mother) of a pet.</summary>
public sealed record GetParentsQuery(Guid PetId) : IRequest<Result<ParentsResponse>>;
