using DogPlatform.Genealogy.Application.Traversal;

namespace DogPlatform.Genealogy.Application.Analysis;

/// <summary>
/// Estimates the kinship (relationship) coefficient between two DIFFERENT pets, using
/// Wright's path-counting method applied directly between the two pets (rather than
/// between a pet's father and mother, as <see cref="IInbreedingCalculator"/> does).
/// </summary>
public interface IKinshipCalculator
{
    /// <summary>
    /// Estimates the kinship coefficient between <paramref name="petId1"/> and
    /// <paramref name="petId2"/>. <paramref name="graph1"/> and <paramref name="graph2"/>
    /// must be ancestor graphs rooted at each pet respectively (they may be the same graph
    /// instance when both pets share a common traversal, e.g. one is an ancestor of the
    /// other's tree).
    /// </summary>
    KinshipResult Calculate(
        Guid petId1,
        AncestorGraph graph1,
        Guid petId2,
        AncestorGraph graph2,
        int maxDepth);
}
