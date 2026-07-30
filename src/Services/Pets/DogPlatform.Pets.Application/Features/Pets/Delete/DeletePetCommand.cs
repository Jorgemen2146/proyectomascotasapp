using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.Delete;

public sealed record DeletePetCommand(Guid PetId) : IRequest<Result>;
