using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.GetMine;

public sealed record GetMyPetsQuery : IRequest<Result<IReadOnlyCollection<MyPetResponse>>>;
