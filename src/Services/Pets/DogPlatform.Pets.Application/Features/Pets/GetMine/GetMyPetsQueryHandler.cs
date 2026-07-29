using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.GetMine;

public sealed class GetMyPetsQueryHandler : IRequestHandler<GetMyPetsQuery, Result<IReadOnlyCollection<MyPetResponse>>>
{
    private readonly IPetRepository _petRepository;
    private readonly ICurrentUser _currentUser;

    public GetMyPetsQueryHandler(IPetRepository petRepository, ICurrentUser currentUser)
    {
        _petRepository = petRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyCollection<MyPetResponse>>> Handle(
        GetMyPetsQuery request,
        CancellationToken cancellationToken)
    {
        var pets = await _petRepository.GetByOwnerIdAsync(_currentUser.UserId, cancellationToken);

        var responses = pets
            .Select(p => new MyPetResponse(p.Id, p.BreedId, p.Name, p.BirthDate, p.Gender.Value))
            .ToList()
            .AsReadOnly();

        return Result.Success(responses);
    }
}
