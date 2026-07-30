using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Species.GetAll;

public sealed class GetAllSpeciesQueryHandler
    : IRequestHandler<GetAllSpeciesQuery, Result<IReadOnlyCollection<SpeciesResponse>>>
{
    private readonly ISpeciesRepository _speciesRepository;

    public GetAllSpeciesQueryHandler(ISpeciesRepository speciesRepository)
    {
        _speciesRepository = speciesRepository;
    }

    public async Task<Result<IReadOnlyCollection<SpeciesResponse>>> Handle(
        GetAllSpeciesQuery request,
        CancellationToken cancellationToken)
    {
        var species = await _speciesRepository.GetAllAsync(cancellationToken);

        var responses = species
            .Select(s => new SpeciesResponse(s.Id, s.Name))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyCollection<SpeciesResponse>>(responses);
    }
}
