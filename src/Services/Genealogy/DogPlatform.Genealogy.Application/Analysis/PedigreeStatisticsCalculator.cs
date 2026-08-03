using DogPlatform.Genealogy.Application.Traversal;

namespace DogPlatform.Genealogy.Application.Analysis;

/// <summary>
/// Computes pedigree completeness, generation distribution and repeated-ancestor
/// statistics by enumerating every position in the binary ancestor tree via
/// <see cref="IGenealogyTraversalService.EnumeratePositions"/>, then delegates the
/// estimated inbreeding coefficient to <see cref="IInbreedingCalculator"/>.
///
/// Complexity: O(2^requestedDepth) positions (bounded by MaximumAnalysisDepth, small),
/// plus the inbreeding calculation cost documented in <see cref="WrightInbreedingCalculator"/>.
/// </summary>
public sealed class PedigreeStatisticsCalculator : IPedigreeStatisticsCalculator
{
    private readonly IGenealogyTraversalService _traversal;
    private readonly IInbreedingCalculator _inbreedingCalculator;

    public PedigreeStatisticsCalculator(
        IGenealogyTraversalService traversal,
        IInbreedingCalculator inbreedingCalculator)
    {
        _traversal = traversal;
        _inbreedingCalculator = inbreedingCalculator;
    }

    public LineageStatisticsResult Calculate(Guid petId, AncestorGraph graph, int requestedDepth)
    {
        var warnings = new List<string>();
        var processedDepth = Math.Min(requestedDepth, graph.MaxDepth);

        var positions = _traversal.EnumeratePositions(petId, processedDepth, graph);

        var totalPositions = positions.Count;
        var knownPositions = positions.Count(p => p.PetId is not null);
        var missingPositions = totalPositions - knownPositions;

        var byGeneration = positions
            .GroupBy(p => p.Generation)
            .OrderBy(g => g.Key)
            .Select(g => new GenerationDistribution(
                g.Key,
                ExpectedPositions: (int)Math.Pow(2, g.Key),
                KnownPositions: g.Count(p => p.PetId is not null),
                AncestorIds: g.Where(p => p.PetId is not null).Select(p => p.PetId!.Value).ToList()))
            .ToList();

        var occurrencesByAncestor = positions
            .Where(p => p.PetId is not null)
            .GroupBy(p => p.PetId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var uniqueAncestorCount = occurrencesByAncestor.Count;

        InbreedingResult? inbreeding = null;
        if (graph.Lineages.ContainsKey(petId))
        {
            inbreeding = _inbreedingCalculator.Calculate(petId, graph, processedDepth);
            warnings.AddRange(inbreeding.Warnings);
        }
        else
        {
            warnings.Add("No hay datos de linaje registrados para esta mascota.");
        }

        var repeated = occurrencesByAncestor
            .Where(kv => kv.Value.Count > 1)
            .Select(kv => new RepeatedAncestor(
                AncestorPetId: kv.Key,
                OccurrenceCount: kv.Value.Count,
                Generations: kv.Value.Select(p => p.Generation).OrderBy(g => g).ToList(),
                LineagePaths: kv.Value.Select(p => p.Path).OrderBy(p => p, StringComparer.Ordinal).ToList(),
                Contribution: null)) // per-ancestor contribution requires re-deriving path pairs; not split out in v3
            .OrderByDescending(r => r.OccurrenceCount)
            .ToList();

        var completeness = totalPositions == 0
            ? 0m
            : Math.Round((decimal)knownPositions / totalPositions * 100m, 2);

        if (requestedDepth > graph.MaxDepth)
            warnings.Add("La profundidad solicitada excede la profundidad configurada/procesada.");

        if (graph.NodeLimitExceeded)
            warnings.Add("El límite máximo de nodos analizados fue alcanzado; las estadísticas pueden estar incompletas.");

        warnings.Add(
            "Las estadísticas de linaje son estimaciones basadas exclusivamente en los datos " +
            "genealógicos registrados en la plataforma y no constituyen un diagnóstico " +
            "veterinario ni genético definitivo.");

        return new LineageStatisticsResult(
            PetId: petId,
            RequestedDepth: requestedDepth,
            ProcessedDepth: processedDepth,
            TotalPositions: totalPositions,
            KnownAncestorPositions: knownPositions,
            MissingAncestorPositions: missingPositions,
            UniqueAncestorCount: uniqueAncestorCount,
            RepeatedAncestorCount: repeated.Count,
            PedigreeCompletenessPercentage: completeness,
            AncestorsByGeneration: byGeneration,
            RepeatedAncestors: repeated,
            EstimatedInbreedingCoefficientPercentage: inbreeding?.Percentage ?? 0m,
            CalculationMethod: inbreeding?.CalculationMethod ?? WrightInbreedingCalculator.MethodDescription,
            Warnings: warnings);
    }
}
