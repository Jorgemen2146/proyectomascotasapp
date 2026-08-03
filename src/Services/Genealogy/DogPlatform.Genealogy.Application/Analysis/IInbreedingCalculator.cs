using DogPlatform.Genealogy.Application.Traversal;

namespace DogPlatform.Genealogy.Application.Analysis;

/// <summary>
/// Calculates the estimated inbreeding coefficient (F) of a pet from its ancestor graph,
/// using Wright's path-counting method.
/// </summary>
public interface IInbreedingCalculator
{
    /// <summary>
    /// Estimates F for <paramref name="petId"/> given the ancestor graph rooted at it
    /// (must already contain <paramref name="petId"/>'s own lineage record so its
    /// father/mother can be resolved), walking up to <paramref name="maxDepth"/>
    /// generations from each parent.
    /// </summary>
    InbreedingResult Calculate(Guid petId, AncestorGraph graph, int maxDepth);
}
