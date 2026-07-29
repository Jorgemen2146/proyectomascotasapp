using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.GetById;

public sealed record GetPetByIdQuery(Guid PetId) : IRequest<Result<PetDetailsResponse>>;
