using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.GetHealthContext;

public sealed record GetPetHealthContextQuery(Guid PetId)
    : IRequest<Result<PetHealthContextResponse>>;

public sealed record PetHealthContextResponse(
    Guid PetId,
    int SpeciesId,
    DateTime? BirthDate,
    string Name);
