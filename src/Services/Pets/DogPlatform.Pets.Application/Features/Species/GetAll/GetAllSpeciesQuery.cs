using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Species.GetAll;

public sealed record GetAllSpeciesQuery : IRequest<Result<IReadOnlyCollection<SpeciesResponse>>>;
