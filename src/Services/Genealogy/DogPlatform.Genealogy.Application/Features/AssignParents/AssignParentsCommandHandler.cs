using DogPlatform.Genealogy.Application.Options;
using DogPlatform.Genealogy.Application.Security;
using DogPlatform.Genealogy.Application.Services;
using DogPlatform.Genealogy.Application.Validation;
using DogPlatform.Genealogy.Domain.Aggregates.PetLineage;
using DogPlatform.Genealogy.Domain.Errors;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.Extensions.Options;

namespace DogPlatform.Genealogy.Application.Features.AssignParents;

public sealed class AssignParentsCommandHandler : IRequestHandler<AssignParentsCommand, Result>
{
    private readonly IPetLineageRepository _lineageRepo;
    private readonly IGenealogyUnitOfWork _unitOfWork;
    private readonly IPetVerificationService _petVerification;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;
    private readonly GenealogyOptions _options;

    public AssignParentsCommandHandler(
        IPetLineageRepository lineageRepo,
        IGenealogyUnitOfWork unitOfWork,
        IPetVerificationService petVerification,
        ICurrentUser currentUser,
        TimeProvider time,
        IOptions<GenealogyOptions> options)
    {
        _lineageRepo      = lineageRepo;
        _unitOfWork       = unitOfWork;
        _petVerification  = petVerification;
        _currentUser      = currentUser;
        _time             = time;
        _options          = options.Value;
    }

    public async Task<Result> Handle(AssignParentsCommand request, CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId;

        // 1. Verify the pet exists and belongs to the authenticated user.
        var petBelongs = await _petVerification.PetBelongsToOwnerAsync(
            request.PetId, ownerId, cancellationToken);

        if (!petBelongs)
            return Result.Failure(GenealogyErrors.Unauthorized);

        // 2. Verify father exists (if provided).
        if (request.FatherId.HasValue)
        {
            var fatherExists = await _petVerification.PetExistsAsync(
                request.FatherId.Value, cancellationToken);

            if (!fatherExists)
                return Result.Failure(GenealogyErrors.FatherNotFound);
        }

        // 3. Verify mother exists (if provided).
        if (request.MotherId.HasValue)
        {
            var motherExists = await _petVerification.PetExistsAsync(
                request.MotherId.Value, cancellationToken);

            if (!motherExists)
                return Result.Failure(GenealogyErrors.MotherNotFound);
        }

        // 4. Prevent circular lineages (petId cannot be an ancestor of the new father/mother).
        var cycleCheck = await LineageCycleGuard.EnsureNoCycleAsync(
            request.PetId, request.FatherId, request.MotherId, _lineageRepo, _options, cancellationToken);

        if (cycleCheck.IsFailure)
            return cycleCheck;

        var now = _time.GetUtcNow().UtcDateTime;

        // 5. Load or create lineage record.
        var lineage = await _lineageRepo.GetByPetIdAsync(request.PetId, cancellationToken);

        if (lineage is null)
        {
            var createResult = PetLineage.Create(
                request.PetId, ownerId, request.FatherId, request.MotherId, now);

            if (createResult.IsFailure)
                return Result.Failure(createResult.Error);

            await _lineageRepo.AddAsync(createResult.Value, cancellationToken);
        }
        else
        {
            var assignResult = lineage.AssignParents(request.FatherId, request.MotherId, now);

            if (assignResult.IsFailure)
                return Result.Failure(assignResult.Error);

            await _lineageRepo.UpdateAsync(lineage, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
