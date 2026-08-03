using DogPlatform.Genealogy.Application.Traversal;

namespace DogPlatform.Genealogy.Application.Analysis;

/// <summary>
/// Calculates pedigree completeness and ancestor distribution statistics for a pet,
/// reusing <see cref="IGenealogyTraversalService"/> for the underlying traversal and
/// <see cref="IInbreedingCalculator"/> for the estimated inbreeding coefficient.
/// </summary>
public interface IPedigreeStatisticsCalculator
{
    LineageStatisticsResult Calculate(Guid petId, AncestorGraph graph, int requestedDepth);
}
