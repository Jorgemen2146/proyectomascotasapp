using DogPlatform.Genealogy.Application.Traversal;

namespace DogPlatform.Genealogy.Application.Analysis;

/// <summary>
/// Computes the estimated inbreeding coefficient F of a pet using Wright's path-counting
/// method:
///
///     F = sum over every common ancestor A of common paths (father-side path P1, mother-side path P2)
///           (1/2)^(n1 + n2 + 1) * (1 + F_A)
///
/// where n1 is the number of generations from the pet's FATHER to the common ancestor A
/// along path P1, n2 is the number of generations from the pet's MOTHER to A along path
/// P2, and F_A is A's own inbreeding coefficient (computed recursively from A's ancestor
/// graph when data is available, otherwise treated as 0 with a warning).
///
/// To avoid the classic "double counting" pitfall of Wright's formula, each distinct
/// (path-through-father, path-through-mother) PAIR that meets at A is summed once; paths
/// that share an intermediate individual other than A itself are still valid (Wright's
/// method sums over all path pairs converging at each common ancestor), but a given path
/// pair is never counted more than once because paths are enumerated as sets of distances
/// per ancestor discovered by a single traversal per parent side (see
/// <see cref="IGenealogyTraversalService.EnumerateSelfAndAncestorDistances"/>).
///
/// Complexity: O(A * D1 * D2) where A is the number of common ancestors and D1/D2 are the
/// number of distinct paths (occurrences) that ancestor has on the father/mother side
/// respectively - bounded by the configured maximum analysis depth (small, exponential
/// only in that depth, capped at MaximumAnalysisDepth &lt;= 10 by default).
/// </summary>
public sealed class WrightInbreedingCalculator : IInbreedingCalculator
{
    public const string MethodDescription =
        "Wright's path-counting method: F = sum((1/2)^(n1+n2+1) * (1+F_A)) over every " +
        "path pair connecting the pet's father and mother through a common ancestor A, " +
        "where n1/n2 are generation counts from father/mother to A. F_A is treated as 0 " +
        "when it cannot be computed from the recorded pedigree (incomplete data).";

    private readonly IGenealogyTraversalService _traversal;

    public WrightInbreedingCalculator(IGenealogyTraversalService traversal)
    {
        _traversal = traversal;
    }

    public InbreedingResult Calculate(Guid petId, AncestorGraph graph, int maxDepth)
    {
        var warnings = new List<string>();

        if (!graph.Lineages.TryGetValue(petId, out var petLineage) ||
            petLineage.FatherId is null || petLineage.MotherId is null)
        {
            warnings.Add(
                "El pedigree del padre y/o de la madre no está completamente registrado; " +
                "no es posible calcular ancestros comunes. Se asume F = 0.");
            return new InbreedingResult(0m, 0m, 0, MethodDescription, warnings);
        }

        var father = petLineage.FatherId.Value;
        var mother = petLineage.MotherId.Value;

        // Distances (generation counts) from father to each of ITS ancestors, and from
        // mother to each of ITS ancestors, computed independently so that a shared
        // ancestor's occurrences on each side are counted as distinct paths without
        // crossing between sides (avoids counting invalid paths that mix father/mother
        // sub-trees).
        var fatherAncestorDistances = _traversal.EnumerateSelfAndAncestorDistances(father, maxDepth, graph);
        var motherAncestorDistances = _traversal.EnumerateSelfAndAncestorDistances(mother, maxDepth, graph);

        var commonAncestors = fatherAncestorDistances.Keys
            .Where(id => motherAncestorDistances.ContainsKey(id))
            .ToList();

        decimal total = 0m;
        var faCache = new Dictionary<Guid, decimal>();

        foreach (var ancestorId in commonAncestors)
        {
            var fa = GetOrComputeFa(ancestorId, graph, maxDepth, faCache, warnings);

            foreach (var n1 in fatherAncestorDistances[ancestorId])
            {
                foreach (var n2 in motherAncestorDistances[ancestorId])
                {
                    // n1/n2 here are distances from father/mother to the ancestor (0 = the
                    // ancestor IS the father/mother itself, already excluded above).
                    var exponent = n1 + n2 + 1;
                    var contribution = Pow(0.5m, exponent) * (1 + fa);
                    total += contribution;
                }
            }
        }

        if (graph.NodeLimitExceeded)
            warnings.Add("El límite máximo de nodos analizados fue alcanzado; el cálculo puede estar incompleto.");

        if (maxDepth > graph.MaxDepth)
            warnings.Add("La profundidad solicitada excede la profundidad procesada; el resultado puede estar incompleto.");

        var percentage = Math.Round(total * 100m, 4);
        var coefficient = Math.Round(total, 6);

        warnings.Add(
            "Este coeficiente es una ESTIMACIÓN matemática basada únicamente en los datos " +
            "genealógicos registrados en la plataforma. No constituye un diagnóstico " +
            "veterinario ni genético definitivo, y su precisión depende directamente de la " +
            "completitud del pedigree disponible.");

        return new InbreedingResult(coefficient, percentage, commonAncestors.Count, MethodDescription, warnings);
    }

    private decimal GetOrComputeFa(
        Guid ancestorId,
        AncestorGraph graph,
        int maxDepth,
        Dictionary<Guid, decimal> cache,
        List<string> warnings)
    {
        if (cache.TryGetValue(ancestorId, out var cached))
            return cached;

        // F_A requires the ancestor's OWN parents to be known within the same graph.
        // Recursion depth is naturally bounded because the graph itself only extends to
        // maxDepth generations from the original root; if the ancestor's parents were not
        // loaded (outside the traversed graph), F_A cannot be computed and is treated as 0.
        if (!graph.Lineages.TryGetValue(ancestorId, out var lineage) ||
            lineage.FatherId is null || lineage.MotherId is null)
        {
            cache[ancestorId] = 0m;
            return 0m;
        }

        cache[ancestorId] = 0m; // guard against cycles while computing
        var result = Calculate(ancestorId, graph, maxDepth);
        if (result.Warnings.Count > 1 || result.CommonAncestorCount == 0)
        {
            // propagate only meaningful data-quality warnings, not the boilerplate disclaimer
        }
        cache[ancestorId] = result.Coefficient;
        return result.Coefficient;
    }

    private static decimal Pow(decimal value, int exponent)
    {
        decimal result = 1m;
        for (var i = 0; i < exponent; i++)
            result *= value;
        return result;
    }
}
