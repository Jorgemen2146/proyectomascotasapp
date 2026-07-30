using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Breeds.GetBySpecies;

public sealed record GetBreedsBySpeciesQuery(int SpeciesId)
    : IRequest<Result<IReadOnlyCollection<BreedResponse>>>;
