using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.GetHealthContext;

public sealed class GetPetHealthContextQueryHandler
    : IRequestHandler<GetPetHealthContextQuery, Result<PetHealthContextResponse>>
{
    private readonly IPetRepository _pets;
    private readonly IBreedRepository _breeds;
    private readonly ICurrentUser _currentUser;

    public GetPetHealthContextQueryHandler(
        IPetRepository pets,
        IBreedRepository breeds,
        ICurrentUser currentUser)
        => (_pets, _breeds, _currentUser) = (pets, breeds, currentUser);

    public async Task<Result<PetHealthContextResponse>> Handle(
        GetPetHealthContextQuery request,
        CancellationToken cancellationToken)
    {
        var pet = await _pets.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<PetHealthContextResponse>(PetErrors.NotFound);
        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure<PetHealthContextResponse>(PetErrors.Unauthorized);

        var breed = await _breeds.GetByIdAsync(pet.BreedId, cancellationToken);
        if (breed is null)
            return Result.Failure<PetHealthContextResponse>(PetErrors.BreedNotFound);

        return Result.Success(new PetHealthContextResponse(
            pet.Id,
            breed.SpeciesId,
            pet.BirthDate,
            pet.Name));
    }
}
