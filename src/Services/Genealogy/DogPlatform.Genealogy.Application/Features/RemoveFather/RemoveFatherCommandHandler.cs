using DogPlatform.Genealogy.Application.Security;
using DogPlatform.Genealogy.Application.Services;
using DogPlatform.Genealogy.Domain.Errors;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.RemoveFather;

public sealed class RemoveFatherCommandHandler : IRequestHandler<RemoveFatherCommand, Result>
{
    private readonly IPetLineageRepository _lineageRepo;
    private readonly IGenealogyUnitOfWork _unitOfWork;
    private readonly IPetVerificationService _petVerification;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public RemoveFatherCommandHandler(
        IPetLineageRepository lineageRepo,
        IGenealogyUnitOfWork unitOfWork,
        IPetVerificationService petVerification,
        ICurrentUser currentUser,
        TimeProvider time)
    {
        _lineageRepo     = lineageRepo;
        _unitOfWork      = unitOfWork;
        _petVerification = petVerification;
        _currentUser     = currentUser;
        _time            = time;
    }

    public async Task<Result> Handle(RemoveFatherCommand request, CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId;

        var petBelongs = await _petVerification.PetBelongsToOwnerAsync(
            request.PetId, ownerId, cancellationToken);

        if (!petBelongs)
            return Result.Failure(GenealogyErrors.Unauthorized);

        var lineage = await _lineageRepo.GetByPetIdAsync(request.PetId, cancellationToken);

        if (lineage is null)
            return Result.Failure(GenealogyErrors.LineageNotFound);

        var result = lineage.RemoveFather(_time.GetUtcNow().UtcDateTime);

        if (result.IsFailure)
            return result;

        await _lineageRepo.UpdateAsync(lineage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
