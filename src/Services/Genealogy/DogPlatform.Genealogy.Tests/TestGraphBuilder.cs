using DogPlatform.Genealogy.Application.Analysis;
using DogPlatform.Genealogy.Application.Traversal;
using DogPlatform.Genealogy.Domain.Aggregates.PetLineage;
using Xunit;

namespace DogPlatform.Genealogy.Tests;

/// <summary>
/// Test helper to build small, deterministic ancestor graphs without a database, and
/// convenience factory for the calculators under test (both are pure/DB-free by design).
/// </summary>
public static class TestGraphBuilder
{
    public static PetLineage Lineage(Guid petId, Guid? father, Guid? mother, Guid owner) =>
        PetLineage.Create(petId, owner, father, mother, DateTime.UtcNow).Value;

    public static AncestorGraph Graph(int maxDepth, params PetLineage[] lineages) =>
        new(
            RootPetId: lineages.Length > 0 ? lineages[0].PetId : Guid.Empty,
            MaxDepth: maxDepth,
            ReachedDepth: maxDepth,
            Lineages: lineages.ToDictionary(l => l.PetId),
            NodeLimitExceeded: false);

    public static IGenealogyTraversalService Traversal() => new GenealogyTraversalService(new NullRepository());

    public static IInbreedingCalculator InbreedingCalculator() => new WrightInbreedingCalculator(Traversal());

    public static IKinshipCalculator KinshipCalculator() =>
        new WrightKinshipCalculator(Traversal(), InbreedingCalculator());

    // The traversal service's async BuildAncestorGraphAsync is not exercised by these
    // math tests (graphs are constructed directly), so the repository is never invoked.
    private sealed class NullRepository : DogPlatform.Genealogy.Domain.Repositories.IPetLineageRepository
    {
        public Task<PetLineage?> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(PetLineage lineage, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(PetLineage lineage, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PetLineage>> GetByPetIdsAsync(
            IEnumerable<Guid> petIds, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PetLineage>> GetChildrenByParentIdsAsync(
            IEnumerable<Guid> parentIds, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
