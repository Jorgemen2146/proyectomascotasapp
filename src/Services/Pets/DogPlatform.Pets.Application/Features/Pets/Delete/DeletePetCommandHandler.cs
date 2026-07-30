using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Domain.Errors;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.Delete;

public sealed class DeletePetCommandHandler : IRequestHandler<DeletePetCommand, Result>
{
    private readonly IPetRepository _petRepository;
    private readonly IPetsUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public DeletePetCommandHandler(
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

    public async Task<Result> Handle(
        DeletePetCommand request,
        CancellationToken cancellationToken)
    {
        var pet = await _petRepository.GetByIdAsync(request.PetId, cancellationToken);

        if (pet is null)
            return Result.Failure(PetErrors.NotFound);

        if (pet.OwnerId != _currentUser.UserId)
            return Result.Failure(PetErrors.Unauthorized);

        var now = _timeProvider.GetUtcNow().DateTime;
        var deleteResult = pet.Delete(now);

        if (deleteResult.IsFailure)
            return deleteResult;

        await _petRepository.UpdateAsync(pet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
