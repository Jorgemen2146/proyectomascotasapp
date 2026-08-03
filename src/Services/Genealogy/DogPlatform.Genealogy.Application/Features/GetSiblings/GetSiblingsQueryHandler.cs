using DogPlatform.Genealogy.Application.Security;
using DogPlatform.Genealogy.Application.Services;
using DogPlatform.Genealogy.Domain.Errors;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.GetSiblings;

public sealed class GetSiblingsQueryHandler
    : IRequestHandler<GetSiblingsQuery, Result<IReadOnlyList<SiblingResponse>>>
{
    private readonly IPetLineageRepository _lineageRepo;
    private readonly IPetVerificationService _petVerification;
    private readonly ICurrentUser _currentUser;

    public GetSiblingsQueryHandler(
        IPetLineageRepository lineageRepo,
        IPetVerificationService petVerification,
        ICurrentUser currentUser)
    {
        _lineageRepo     = lineageRepo;
        _petVerification = petVerification;
        _currentUser     = currentUser;
    }

    public async Task<Result<IReadOnlyList<SiblingResponse>>> Handle(
        GetSiblingsQuery request,
        CancellationToken cancellationToken)
    {
        // Privacy policy: only the owner of the pet may query its calculated siblings.
        var owns = await _petVerification.PetBelongsToOwnerAsync(
            request.PetId, _currentUser.UserId, cancellationToken);

        if (!owns)
            return Result.Failure<IReadOnlyList<SiblingResponse>>(GenealogyErrors.Unauthorized);

        var rootLineage = await _lineageRepo.GetByPetIdAsync(request.PetId, cancellationToken);

        // A pet with no known parents cannot have siblings.
        if (rootLineage is null || (rootLineage.FatherId is null && rootLineage.MotherId is null))
            return Result.Success<IReadOnlyList<SiblingResponse>>(Array.Empty<SiblingResponse>());

        var parentIds = new List<Guid>();
        if (rootLineage.FatherId is Guid f) parentIds.Add(f);
        if (rootLineage.MotherId is Guid m) parentIds.Add(m);

        var candidates = await _lineageRepo.GetChildrenByParentIdsAsync(parentIds, cancellationToken);

        var siblings = new List<SiblingResponse>();

        foreach (var candidate in candidates)
        {
            if (candidate.PetId == request.PetId)
                continue;

            var sameFather = rootLineage.FatherId.HasValue &&
                              candidate.FatherId.HasValue &&
                              candidate.FatherId.Value == rootLineage.FatherId.Value;

            var sameMother = rootLineage.MotherId.HasValue &&
                              candidate.MotherId.HasValue &&
                              candidate.MotherId.Value == rootLineage.MotherId.Value;

            if (sameFather && sameMother)
            {
                siblings.Add(new SiblingResponse(candidate.PetId, SiblingRelationship.FullSibling));
            }
            else if (sameFather)
            {
                siblings.Add(new SiblingResponse(candidate.PetId, SiblingRelationship.HalfSiblingByFather));
            }
            else if (sameMother)
            {
                siblings.Add(new SiblingResponse(candidate.PetId, SiblingRelationship.HalfSiblingByMother));
            }
            // Otherwise the candidate only matched because GetChildrenByParentIdsAsync
            // returned it for the other parent id in the batch; it does not actually
            // share a parent with the root pet, so it is not a sibling.
        }

        siblings.Sort((a, b) =>
        {
            var byRelationship = a.Relationship.CompareTo(b.Relationship);
            return byRelationship != 0 ? byRelationship : a.PetId.CompareTo(b.PetId);
        });

        return Result.Success<IReadOnlyList<SiblingResponse>>(siblings);
    }
}
