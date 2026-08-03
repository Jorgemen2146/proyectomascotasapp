using DogPlatform.Genealogy.Domain.Aggregates.PetLineage;

namespace DogPlatform.Genealogy.Application.Traversal;

/// <summary>
/// Immutable snapshot of the ancestor lineage graph rooted at <see cref="RootPetId"/>,
/// containing every <see cref="PetLineage"/> record reachable by walking father/mother
/// links up to <see cref="MaxDepth"/> generations, loaded in one batched query per
/// generation (see <see cref="IGenealogyTraversalService"/>).
/// </summary>
/// <param name="RootPetId">The pet the graph was built for.</param>
/// <param name="MaxDepth">The depth that was requested when building the graph.</param>
/// <param name="ReachedDepth">
/// The deepest generation actually processed before traversal stopped (equal to
/// <see cref="MaxDepth"/> unless the node-count safety cap was hit first).
/// </param>
/// <param name="Lineages">PetId -> PetLineage for every pet found during the traversal.</param>
/// <param name="NodeLimitExceeded">
/// True if the traversal stopped early because it reached the configured
/// MaximumTraversalNodes safety cap (data may be incomplete beyond this point).
/// </param>
public sealed record AncestorGraph(
    Guid RootPetId,
    int MaxDepth,
    int ReachedDepth,
    IReadOnlyDictionary<Guid, PetLineage> Lineages,
    bool NodeLimitExceeded);

/// <summary>
/// A single ancestor "slot" in the binary pedigree tree rooted at a pet.
/// Generation 1 = father/mother, generation 2 = grandparents, etc.
/// <see cref="PetId"/> is null when that ancestor is unknown (missing data).
/// </summary>
public sealed record AncestorPosition(Guid? PetId, int Generation, string Path);
