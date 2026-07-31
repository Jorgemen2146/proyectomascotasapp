using DogPlatform.Genealogy.Domain.Errors;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.GetParents;

public sealed class GetParentsQueryHandler : IRequestHandler<GetParentsQuery, Result<ParentsResponse>>
{
    private readonly IPetLineageRepository _lineageRepo;

    public GetParentsQueryHandler(IPetLineageRepository lineageRepo)
    {
        _lineageRepo = lineageRepo;
    }

    public async Task<Result<ParentsResponse>> Handle(
        GetParentsQuery request,
        CancellationToken cancellationToken)
    {
        var lineage = await _lineageRepo.GetByPetIdAsync(request.PetId, cancellationToken);

        if (lineage is null)
            return Result.Failure<ParentsResponse>(GenealogyErrors.LineageNotFound);

        var response = new ParentsResponse(
            lineage.PetId,
            lineage.FatherId,
            lineage.MotherId,
            lineage.CreatedAt,
            lineage.UpdatedAt);

        return Result.Success(response);
    }
}
