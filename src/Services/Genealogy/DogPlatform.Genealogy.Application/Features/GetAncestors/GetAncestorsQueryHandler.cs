using DogPlatform.Genealogy.Application.Options;
using DogPlatform.Genealogy.Application.Security;
using DogPlatform.Genealogy.Application.Services;
using DogPlatform.Genealogy.Domain.Errors;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.Extensions.Options;

namespace DogPlatform.Genealogy.Application.Features.GetAncestors;

public sealed class GetAncestorsQueryHandler
    : IRequestHandler<GetAncestorsQuery, Result<IReadOnlyList<AncestorResponse>>>
{
    private readonly IPetLineageRepository _lineageRepo;
    private readonly IPetVerificationService _petVerification;
    private readonly ICurrentUser _currentUser;
    private readonly GenealogyOptions _options;

    public GetAncestorsQueryHandler(
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

    public async Task<Result<IReadOnlyList<AncestorResponse>>> Handle(
        GetAncestorsQuery request,
        CancellationToken cancellationToken)
    {
        var depth = Math.Clamp(request.Depth ?? _options.DefaultTreeDepth, 1, _options.MaximumTreeDepth);

        // Privacy policy: only the owner of the root pet may query its ancestors.
        var owns = await _petVerification.PetBelongsToOwnerAsync(
            request.PetId, _currentUser.UserId, cancellationToken);

        if (!owns)
            return Result.Failure<IReadOnlyList<AncestorResponse>>(GenealogyErrors.Unauthorized);

        var rootLineage = await _lineageRepo.GetByPetIdAsync(request.PetId, cancellationToken);

        var results = new List<AncestorResponse>();

        if (rootLineage is null)
            return Result.Success<IReadOnlyList<AncestorResponse>>(results);

        // frontier: petId -> list of lineage paths that reach it at the current generation
        var frontier = new Dictionary<Guid, List<string>>();

        if (rootLineage.FatherId is Guid fatherId)
            AddPath(frontier, fatherId, "father");
        if (rootLineage.MotherId is Guid motherId)
            AddPath(frontier, motherId, "mother");

        var visited = new HashSet<Guid>();
        var generation = 1;

        while (frontier.Count > 0 && generation <= depth)
        {
            if (visited.Count + frontier.Count > _options.MaximumTraversalNodes)
                return Result.Failure<IReadOnlyList<AncestorResponse>>(GenealogyErrors.MaximumTraversalExceeded);

            var lineages = await _lineageRepo.GetByPetIdsAsync(frontier.Keys, cancellationToken);
            var lineageMap = lineages.ToDictionary(l => l.PetId);

            var currentGeneration = generation;
            foreach (var (petId, paths) in frontier)
            {
                results.Add(new AncestorResponse(petId, paths[0], currentGeneration, paths));
                visited.Add(petId);
            }

            var nextFrontier = new Dictionary<Guid, List<string>>();

            foreach (var (petId, paths) in frontier)
            {
                if (!lineageMap.TryGetValue(petId, out var lineage))
                    continue;

                if (lineage.FatherId is Guid f && !visited.Contains(f))
                    foreach (var path in paths)
                        AddPath(nextFrontier, f, $"{path}.father");

                if (lineage.MotherId is Guid m && !visited.Contains(m))
                    foreach (var path in paths)
                        AddPath(nextFrontier, m, $"{path}.mother");
            }

            frontier = nextFrontier;
            generation++;
        }

        results.Sort((a, b) =>
        {
            var byGeneration = a.Generation.CompareTo(b.Generation);
            return byGeneration != 0
                ? byGeneration
                : string.CompareOrdinal(a.LineagePath, b.LineagePath);
        });

        return Result.Success<IReadOnlyList<AncestorResponse>>(results);
    }

    private static void AddPath(Dictionary<Guid, List<string>> map, Guid petId, string path)
    {
        if (!map.TryGetValue(petId, out var paths))
        {
            paths = new List<string>();
            map[petId] = paths;
        }

        paths.Add(path);
    }
}
