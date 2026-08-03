namespace DogPlatform.Genealogy.Application.Features.GetAncestors;

/// <summary>
/// A flattened ancestor entry. If the same ancestor is reachable through more than one
/// branch (e.g. shared through both father and mother lines), <see cref="Paths"/> contains
/// every distinct lineage path and the entry is emitted only once.
/// </summary>
public sealed record AncestorResponse(
    Guid PetId,
    string LineagePath,
    int Generation,
    IReadOnlyList<string> Paths);
