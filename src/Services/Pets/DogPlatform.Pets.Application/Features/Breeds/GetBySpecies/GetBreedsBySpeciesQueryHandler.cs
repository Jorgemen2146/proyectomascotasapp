using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Breeds.GetBySpecies;

public sealed class GetBreedsBySpeciesQueryHandler
    : IRequestHandler<GetBreedsBySpeciesQuery, Result<IReadOnlyCollection<BreedResponse>>>
{
    private readonly ISpeciesRepository _speciesRepository;
    private readonly IBreedRepository _breedRepository;

    public GetBreedsBySpeciesQueryHandler(
        ISpeciesRepository speciesRepository,
        IBreedRepository breedRepository)
    {
        _speciesRepository = speciesRepository;
        _breedRepository = breedRepository;
    }

    public async Task<Result<IReadOnlyCollection<BreedResponse>>> Handle(
        GetBreedsBySpeciesQuery request,
        CancellationToken cancellationToken)
    {
        if (!await _speciesRepository.ExistsAsync(request.SpeciesId, cancellationToken))
            return Result.Failure<IReadOnlyCollection<BreedResponse>>(SpeciesErrors.NotFound);

        var breeds = await _breedRepository.GetBySpeciesIdAsync(request.SpeciesId, cancellationToken);

        var responses = breeds
            .Select(b => new BreedResponse(b.Id, b.SpeciesId, b.Name))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyCollection<BreedResponse>>(responses);
    }
}
