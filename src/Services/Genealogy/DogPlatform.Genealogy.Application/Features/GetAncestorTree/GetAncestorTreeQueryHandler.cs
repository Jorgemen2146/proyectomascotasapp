using DogPlatform.Genealogy.Application.Options;
using DogPlatform.Genealogy.Application.Security;
using DogPlatform.Genealogy.Application.Services;
using DogPlatform.Genealogy.Domain.Aggregates.PetLineage;
using DogPlatform.Genealogy.Domain.Errors;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.Extensions.Options;

namespace DogPlatform.Genealogy.Application.Features.GetAncestorTree;

public sealed class GetAncestorTreeQueryHandler
    : IRequestHandler<GetAncestorTreeQuery, Result<GenealogyTreeResponse>>
{
    private readonly IPetLineageRepository _lineageRepo;
    private readonly IPetVerificationService _petVerification;
    private readonly ICurrentUser _currentUser;
    private readonly GenealogyOptions _options;

    public GetAncestorTreeQueryHandler(
        IPetLineageRepository lineageRepo,
        IPetVerificationService petVerification,
        ICurrentUser currentUser,
        IOptions<GenealogyOptions> options)
    {
        _lineageRepo     = lineageRepo;
        _petVerification = petVerification;
        _currentUser     = currentUser;
        _options         = options.Value;
    }

    public async Task<Result<GenealogyTreeResponse>> Handle(
        GetAncestorTreeQuery request,
        CancellationToken cancellationToken)
    {
        var depth = Math.Clamp(request.Depth ?? _options.DefaultTreeDepth, 1, _options.MaximumTreeDepth);

        // Privacy policy: while Genealogy has no concept of "public" pets, only the
        // owner of the root pet may query its ancestor tree.
        var owns = await _petVerification.PetBelongsToOwnerAsync(
            request.PetId, _currentUser.UserId, cancellationToken);

        if (!owns)
            return Result.Failure<GenealogyTreeResponse>(GenealogyErrors.Unauthorized);

        var lineageMap = new Dictionary<Guid, PetLineage>();

        var rootLineage = await _lineageRepo.GetByPetIdAsync(request.PetId, cancellationToken);
        if (rootLineage is not null)
            lineageMap[request.PetId] = rootLineage;

        var visited = new HashSet<Guid> { request.PetId };
        var currentGeneration = new HashSet<Guid>();

        if (rootLineage?.FatherId is Guid fatherId && visited.Add(fatherId))
            currentGeneration.Add(fatherId);
        if (rootLineage?.MotherId is Guid motherId && visited.Add(motherId))
            currentGeneration.Add(motherId);

        var generation = 1;
        while (currentGeneration.Count > 0 && generation <= depth)
        {
            var lineages = await _lineageRepo.GetByPetIdsAsync(currentGeneration, cancellationToken);
            foreach (var lineage in lineages)
                lineageMap[lineage.PetId] = lineage;

            var nextGeneration = new HashSet<Guid>();
            foreach (var id in currentGeneration)
            {
                if (!lineageMap.TryGetValue(id, out var lineage))
                    continue;

                if (lineage.FatherId is Guid f && visited.Add(f))
                    nextGeneration.Add(f);
                if (lineage.MotherId is Guid m && visited.Add(m))
                    nextGeneration.Add(m);
            }

            if (visited.Count > _options.MaximumTraversalNodes)
                return Result.Failure<GenealogyTreeResponse>(GenealogyErrors.MaximumTraversalExceeded);

            currentGeneration = nextGeneration;
            generation++;
        }

        var root = BuildNode(request.PetId, 0, GenealogyRelationshipType.Root, depth, lineageMap);

        return Result.Success(new GenealogyTreeResponse(request.PetId, depth, root));
    }

    private static GenealogyNodeResponse BuildNode(
        Guid petId,
        int generation,
        GenealogyRelationshipType relationship,
        int depth,
        IReadOnlyDictionary<Guid, PetLineage> lineageMap)
    {
        lineageMap.TryGetValue(petId, out var lineage);

        GenealogyNodeResponse? father = null;
        GenealogyNodeResponse? mother = null;

        if (generation < depth)
        {
            if (lineage?.FatherId is Guid fatherId)
                father = BuildNode(fatherId, generation + 1, GenealogyRelationshipType.Father, depth, lineageMap);

            if (lineage?.MotherId is Guid motherId)
                mother = BuildNode(motherId, generation + 1, GenealogyRelationshipType.Mother, depth, lineageMap);
        }

        return new GenealogyNodeResponse(
            petId,
            Name: null,
            SpeciesId: null,
            BreedId: null,
            Sex: null,
            MainPhotoUrl: null,
            relationship,
            generation,
            father,
            mother);
    }
}
