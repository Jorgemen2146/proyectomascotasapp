using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.GetById;

public sealed class GetPetByIdQueryHandler : IRequestHandler<GetPetByIdQuery, Result<PetDetailsResponse>>
{
    private readonly IPetRepository _petRepository;
    private readonly ICurrentUser _currentUser;

    public GetPetByIdQueryHandler(IPetRepository petRepository, ICurrentUser currentUser)
    {
        _petRepository = petRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<PetDetailsResponse>> Handle(
        GetPetByIdQuery request,
        CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByIdAsync(request.PetId, cancellationToken);

        if (pet is null)
            return Result.Failure<PetDetailsResponse>(PetErrors.NotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure<PetDetailsResponse>(PetErrors.Unauthorized);

        var response = new PetDetailsResponse(
            pet.Id,
            pet.BreedId,
            pet.Name,
            pet.BirthDate,
            pet.Gender.Value,
            pet.Weight,
            pet.Color,
            pet.PedigreeNumber,
            pet.IsSterilized,
            pet.Description,
            pet.CreatedAt,
            pet.UpdatedAt);

        return Result.Success(response);
    }
}
