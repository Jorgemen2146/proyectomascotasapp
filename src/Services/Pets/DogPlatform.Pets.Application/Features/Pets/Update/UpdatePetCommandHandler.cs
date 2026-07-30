using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.Pets.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.Update;

public sealed class UpdatePetCommandHandler : IRequestHandler<UpdatePetCommand, Result<UpdatePetResponse>>
{
    private readonly IPetRepository _petRepository;
    private readonly IPetsUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdatePetCommandHandler(
        IPetRepository petRepository,
        IPetsUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _petRepository = petRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<UpdatePetResponse>> Handle(
        UpdatePetCommand request,
        CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByIdAsync(request.PetId, cancellationToken);

        if (pet is null)
            return Result.Failure<UpdatePetResponse>(PetErrors.NotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure<UpdatePetResponse>(PetErrors.Unauthorized);

        var genderResult = Gender.Create(request.Gender);
        if (genderResult.IsFailure)
            return Result.Failure<UpdatePetResponse>(genderResult.Error);

        var now = _timeProvider.GetUtcNow().DateTime;
        pet.Update(
            request.Name,
            request.BirthDate,
            genderResult.Value,
            request.Weight,
            request.Color,
            request.PedigreeNumber,
            request.IsSterilized,
            request.Description,
            now);

        await _petRepository.UpdateAsync(pet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdatePetResponse(request.PetId, request.Name));
    }
}
