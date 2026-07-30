using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Domain.Aggregates.Pet;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.Pets.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.Create;

public sealed class CreatePetCommandHandler : IRequestHandler<CreatePetCommand, Result<CreatePetResponse>>
{
    private readonly IPetRepository _petRepository;
    private readonly IBreedRepository _breedRepository;
    private readonly IPetsUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreatePetCommandHandler(
        IPetRepository petRepository,
        IBreedRepository breedRepository,
        IPetsUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _petRepository = petRepository;
        _breedRepository = breedRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CreatePetResponse>> Handle(
        CreatePetCommand request,
        CancellationToken cancellationToken)
    {
        var breed = await _breedRepository.GetByIdAsync(request.BreedId, cancellationToken);
        if (breed is null)
            return Result.Failure<CreatePetResponse>(PetErrors.BreedNotFound);

        var genderResult = Gender.Create(request.Gender);
        if (genderResult.IsFailure)
            return Result.Failure<CreatePetResponse>(genderResult.Error);

        var petId = Guid.NewGuid();
        var now = _timeProvider.GetUtcNow().DateTime;

        var petResult = Pet.Create(
            petId,
            _currentUser.UserId,
            request.BreedId,
            request.Name,
            request.BirthDate,
            genderResult.Value,
            request.Weight,
            request.Color,
            request.PedigreeNumber,
            request.IsSterilized,
            request.Description,
            now);

        if (petResult.IsFailure)
            return Result.Failure<CreatePetResponse>(petResult.Error);

        await _petRepository.AddAsync(petResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreatePetResponse(petId, request.Name));
    }
}
