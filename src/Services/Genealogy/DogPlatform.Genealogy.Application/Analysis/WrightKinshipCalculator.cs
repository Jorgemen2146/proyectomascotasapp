using DogPlatform.Genealogy.Application.Traversal;

namespace DogPlatform.Genealogy.Application.Analysis;

/// <summary>
/// Computes the coefficient of coancestry (kinship) f(X, Y) between two pets using
/// Wright's path-counting method:
///
///     f(X, Y) = sum over every common ancestor A of path pairs (X -> A, Y -> A)
///                 (1/2)^(n1 + n2 + 1) * (1 + F_A)
///
/// where n1 is the number of generations from X to A and n2 is the number of generations
/// from Y to A (n = 0 when A is X or Y itself, which correctly handles the direct
/// parent/ancestor case). F_A is A's own inbreeding coefficient, computed via
/// <see cref="IInbreedingCalculator"/> from A's ancestor graph when available, otherwise
/// treated as 0 with a warning.
///
/// When petId1 == petId2 the result degenerates to (1 + F_X) / 2, the standard
/// self-coancestry identity, and is reported with an explanatory warning rather than as a
/// meaningful "relationship".
/// </summary>
public sealed class WrightKinshipCalculator : IKinshipCalculator
{
    public const string MethodDescription =
        "Wright's path-counting method: f(X,Y) = sum((1/2)^(n1+n2+1) * (1+F_A)) over every " +
        "path pair connecting X and Y through a common ancestor A (including X or Y " +
        "themselves when one is a direct ancestor of the other), where n1/n2 are " +
        "generation counts from X/Y to A. F_A is treated as 0 when it cannot be computed " +
        "from the recorded pedigree (incomplete data).";

    private readonly IGenealogyTraversalService _traversal;
    private readonly IInbreedingCalculator _inbreedingCalculator;

    public WrightKinshipCalculator(IGenealogyTraversalService traversal, IInbreedingCalculator inbreedingCalculator)
    {
        _traversal = traversal;
        _inbreedingCalculator = inbreedingCalculator;
    }

    public KinshipResult Calculate(
        Guid petId1,
        AncestorGraph graph1,
        Guid petId2,
        AncestorGraph graph2,
        int maxDepth)
    {
        var warnings = new List<string>();

        if (petId1 == petId2)
        {
            var selfF = graph1.Lineages.ContainsKey(petId1)
                ? _inbreedingCalculator.Calculate(petId1, graph1, maxDepth).Coefficient
                : 0m;

            warnings.Add(
                "Ambas mascotas son la misma; se devuelve el coeficiente de auto-parentesco " +
                "(1 + F) / 2, que no representa un parentesco entre dos individuos distintos.");

            var self = Math.Round((1 + selfF) / 2m, 6);
            return new KinshipResult(self, Math.Round(self * 100m, 4), MethodDescription, warnings);
        }

        var distances1 = _traversal.EnumerateSelfAndAncestorDistances(petId1, maxDepth, graph1);
        var distances2 = _traversal.EnumerateSelfAndAncestorDistances(petId2, maxDepth, graph2);

        var commonIds = distances1.Keys.Where(distances2.ContainsKey).ToList();

        decimal total = 0m;
        var faCache = new Dictionary<Guid, decimal>();

        foreach (var ancestorId in commonIds)
        {
            var fa = GetFa(ancestorId, graph1, graph2, maxDepth, faCache);

            foreach (var n1 in distances1[ancestorId])
            {
                foreach (var n2 in distances2[ancestorId])
                {
                    var exponent = n1 + n2 + 1;
                    total += Pow(0.5m, exponent) * (1 + fa);
                }
            }
        }

        if (graph1.NodeLimitExceeded || graph2.NodeLimitExceeded)
            warnings.Add("El límite máximo de nodos analizados fue alcanzado; el cálculo puede estar incompleto.");

        warnings.Add(
            "Este coeficiente es una ESTIMACIÓN matemática basada únicamente en los datos " +
            "genealógicos registrados en la plataforma. No constituye un diagnóstico " +
            "veterinario ni genético definitivo, y su precisión depende directamente de la " +
            "completitud del pedigree disponible.");

        var coefficient = Math.Round(total, 6);
        return new KinshipResult(coefficient, Math.Round(coefficient * 100m, 4), MethodDescription, warnings);
    }

    private decimal GetFa(
        Guid ancestorId,
        AncestorGraph graph1,
        AncestorGraph graph2,
        int maxDepth,
        Dictionary<Guid, decimal> cache)
    {
        if (cache.TryGetValue(ancestorId, out var cached))
            return cached;

        var graph = graph1.Lineages.ContainsKey(ancestorId) ? graph1 : graph2;

        if (!graph.Lineages.ContainsKey(ancestorId))
        {
            cache[ancestorId] = 0m;
            return 0m;
        }

        cache[ancestorId] = 0m; // guard against recursive cycles
        var result = _inbreedingCalculator.Calculate(ancestorId, graph, maxDepth);
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
