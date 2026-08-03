using DogPlatform.Genealogy.Domain.Aggregates.PetLineage;
using DogPlatform.Genealogy.Domain.Repositories;

namespace DogPlatform.Genealogy.Application.Traversal;

/// <inheritdoc cref="IGenealogyTraversalService"/>
public sealed class GenealogyTraversalService : IGenealogyTraversalService
{
    private readonly IPetLineageRepository _lineageRepo;

    public GenealogyTraversalService(IPetLineageRepository lineageRepo)
    {
        _lineageRepo = lineageRepo;
    }

    public async Task<AncestorGraph> BuildAncestorGraphAsync(
        Guid rootPetId,
        int maxDepth,
        int maxNodes,
        CancellationToken cancellationToken)
    {
        var lineageMap = new Dictionary<Guid, PetLineage>();

        var rootLineage = await _lineageRepo.GetByPetIdAsync(rootPetId, cancellationToken);
        if (rootLineage is not null)
            lineageMap[rootPetId] = rootLineage;

        var visited = new HashSet<Guid> { rootPetId };
        var currentGeneration = new HashSet<Guid>();

        if (rootLineage?.FatherId is Guid fatherId && visited.Add(fatherId))
            currentGeneration.Add(fatherId);
        if (rootLineage?.MotherId is Guid motherId && visited.Add(motherId))
            currentGeneration.Add(motherId);

        var generation = 1;
        var reachedDepth = 0;
        var nodeLimitExceeded = false;

        while (currentGeneration.Count > 0 && generation <= maxDepth)
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

            reachedDepth = generation;

            if (visited.Count > maxNodes)
            {
                nodeLimitExceeded = true;
                break;
            }

            currentGeneration = nextGeneration;
            generation++;
        }

        return new AncestorGraph(rootPetId, maxDepth, reachedDepth, lineageMap, nodeLimitExceeded);
    }

    public IReadOnlyList<AncestorPosition> EnumeratePositions(Guid rootPetId, int maxDepth, AncestorGraph graph)
    {
        var positions = new List<AncestorPosition>();
        Recurse(rootPetId, 1, string.Empty, maxDepth, graph, positions);
        return positions;
    }

    private static void Recurse(
        Guid? petId,
        int generation,
        string parentPath,
        int maxDepth,
        AncestorGraph graph,
        List<AncestorPosition> positions)
    {
        if (generation > maxDepth)
            return;

        Guid? father = null;
        Guid? mother = null;

        if (petId is Guid pid && graph.Lineages.TryGetValue(pid, out var lineage))
        {
            father = lineage.FatherId;
            mother = lineage.MotherId;
        }

        var fatherPath = parentPath.Length == 0 ? "father" : $"{parentPath}.father";
        var motherPath = parentPath.Length == 0 ? "mother" : $"{parentPath}.mother";

        positions.Add(new AncestorPosition(father, generation, fatherPath));
        positions.Add(new AncestorPosition(mother, generation, motherPath));

        Recurse(father, generation + 1, fatherPath, maxDepth, graph, positions);
        Recurse(mother, generation + 1, motherPath, maxDepth, graph, positions);
    }

    public IReadOnlyDictionary<Guid, IReadOnlyList<int>> EnumerateSelfAndAncestorDistances(
        Guid startPetId,
        int maxDistance,
        AncestorGraph graph)
    {
        var result = new Dictionary<Guid, List<int>>();
        Recurse(startPetId, 0, maxDistance, graph, result);
        return result.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<int>)kv.Value);
    }

    private static void Recurse(
        Guid petId,
        int distance,
        int maxDistance,
        AncestorGraph graph,
        Dictionary<Guid, List<int>> result)
    {
        if (!result.TryGetValue(petId, out var list))
        {
            list = new List<int>();
            result[petId] = list;
        }

        list.Add(distance);

        if (distance >= maxDistance)
            return;

        if (!graph.Lineages.TryGetValue(petId, out var lineage))
            return;

        if (lineage.FatherId is Guid f)
            Recurse(f, distance + 1, maxDistance, graph, result);
        if (lineage.MotherId is Guid m)
            Recurse(m, distance + 1, maxDistance, graph, result);
    }
}
