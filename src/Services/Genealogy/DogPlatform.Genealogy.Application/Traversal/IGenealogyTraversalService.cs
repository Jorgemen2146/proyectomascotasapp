namespace DogPlatform.Genealogy.Application.Traversal;

/// <summary>
/// Single reusable service for genealogy tree traversal. All ancestor-based features
/// (tree, flattened ancestors, statistics, inbreeding, relationship) build their working
/// data set through this service instead of re-implementing BFS traversal, to guarantee
/// a single source of truth for cycle-safety, batching and depth/node limits.
/// </summary>
public interface IGenealogyTraversalService
{
    /// <summary>
    /// Builds the ancestor graph rooted at <paramref name="rootPetId"/> by walking
    /// father/mother links generation by generation, loading each generation with a
    /// single batched repository call (no N+1 SQL). Cycle-safe via a visited-set;
    /// stops early (setting <see cref="AncestorGraph.NodeLimitExceeded"/>) once
    /// <paramref name="maxNodes"/> distinct pets have been visited.
    /// Complexity: O(V) repository round trips bounded by generations (not by fan-out),
    /// where each round trip loads at most 2^generation ids in one query.
    /// </summary>
    Task<AncestorGraph> BuildAncestorGraphAsync(
        Guid rootPetId,
        int maxDepth,
        int maxNodes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Pure (no I/O) enumeration of every position in the binary ancestor tree rooted at
    /// <paramref name="rootPetId"/>, from generation 1 up to <paramref name="maxDepth"/>.
    /// Produces exactly 2^1 + 2^2 + ... + 2^maxDepth positions. A position's PetId is null
    /// when the corresponding ancestor is unknown. Complexity: O(2^maxDepth) positions,
    /// bounded by the configured MaximumAnalysisDepth (small, &lt;= 10 by default).
    /// </summary>
    IReadOnlyList<AncestorPosition> EnumeratePositions(Guid rootPetId, int maxDepth, AncestorGraph graph);

    /// <summary>
    /// Pure (no I/O) enumeration of <paramref name="startPetId"/> itself (distance 0) and
    /// every ancestor reachable from it up to <paramref name="maxDistance"/> generations.
    /// Unlike a simple "unique ancestors" set, the SAME ancestor can appear more than once
    /// in the returned lists if it is reachable via more than one branch (this is required
    /// input for Wright's path-counting inbreeding/kinship formulas).
    /// </summary>
    IReadOnlyDictionary<Guid, IReadOnlyList<int>> EnumerateSelfAndAncestorDistances(
        Guid startPetId,
        int maxDistance,
        AncestorGraph graph);
}
