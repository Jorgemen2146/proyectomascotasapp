using DogPlatform.Genealogy.Application.Options;
using DogPlatform.Genealogy.Domain.Errors;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Genealogy.Application.Validation;

/// <summary>
/// Prevents circular genealogical lineages (a pet becoming, directly or transitively,
/// its own ancestor) before a parent assignment is persisted.
///
/// Algorithm: starting from the candidate father/mother, walks UP the ancestor chain
/// (father, mother, grandparents, ...) one generation at a time, loading each generation
/// in a single batched query (<see cref="IPetLineageRepository.GetByPetIdsAsync"/>).
/// If <paramref name="petId"/> is ever found among the visited ancestors, assigning the
/// candidate as a parent of <paramref name="petId"/> would close a cycle, so the operation
/// is rejected.
///
/// A <see cref="HashSet{Guid}"/> of already-visited pets guarantees termination even if the
/// existing data already contains a cycle (corrupted data), and
/// <see cref="GenealogyOptions.MaximumTreeDepth"/> / <see cref="GenealogyOptions.MaximumTraversalNodes"/>
/// provide hard safety caps.
/// </summary>
public static class LineageCycleGuard
{
    public static async Task<Result> EnsureNoCycleAsync(
        Guid petId,
        Guid? fatherId,
        Guid? motherId,
        IPetLineageRepository repository,
        GenealogyOptions options,
        CancellationToken cancellationToken)
    {
        var frontier = new HashSet<Guid>();
        if (fatherId.HasValue)
            frontier.Add(fatherId.Value);
        if (motherId.HasValue)
            frontier.Add(motherId.Value);

        if (frontier.Count == 0)
            return Result.Success();

        var visited = new HashSet<Guid>(frontier);
        var depth = 0;

        while (frontier.Count > 0)
        {
            depth++;
            if (depth > options.MaximumTreeDepth)
                break;

            var lineages = await repository.GetByPetIdsAsync(frontier, cancellationToken);

            var nextFrontier = new HashSet<Guid>();

            foreach (var lineage in lineages)
            {
                if (lineage.FatherId.HasValue)
                {
                    if (lineage.FatherId.Value == petId)
                        return Result.Failure(GenealogyErrors.CircularLineageDetected);

                    if (visited.Add(lineage.FatherId.Value))
                        nextFrontier.Add(lineage.FatherId.Value);
                }

                if (lineage.MotherId.HasValue)
                {
                    if (lineage.MotherId.Value == petId)
                        return Result.Failure(GenealogyErrors.CircularLineageDetected);

                    if (visited.Add(lineage.MotherId.Value))
                        nextFrontier.Add(lineage.MotherId.Value);
                }
            }

            if (visited.Count > options.MaximumTraversalNodes)
                return Result.Failure(GenealogyErrors.MaximumTraversalExceeded);

            frontier = nextFrontier;
        }

        return Result.Success();
    }
}
