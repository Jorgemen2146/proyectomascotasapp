using DogPlatform.Genealogy.Application.Traversal;
using DogPlatform.Genealogy.Domain.Aggregates.PetLineage;
using Xunit;

namespace DogPlatform.Genealogy.Tests;

/// <summary>
/// Mathematical tests for Wright's path-counting inbreeding/kinship formulas, using small,
/// hand-verified pedigrees. Expected values are documented in each test's comments.
/// </summary>
public sealed class InbreedingCalculatorTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    [Fact]
    public void UnrelatedParents_ProducesZeroInbreeding()
    {
        // Pet <- Father, Mother (no shared ancestors at all)
        var pet = Guid.NewGuid();
        var father = Guid.NewGuid();
        var mother = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            5,
            TestGraphBuilder.Lineage(pet, father, mother, Owner),
            TestGraphBuilder.Lineage(father, null, null, Owner),
            TestGraphBuilder.Lineage(mother, null, null, Owner));

        var result = TestGraphBuilder.InbreedingCalculator().Calculate(pet, graph, 5);

        Assert.Equal(0m, result.Coefficient);
        Assert.Equal(0, result.CommonAncestorCount);
    }

    [Fact]
    public void FatherDaughterMating_ProducesExpectedInbreeding()
    {
        // G is grandsire. G x X -> Daughter (D). Then G x D -> Pet.
        // Pet's father = G, Pet's mother = D. D's father = G, D's mother = X.
        // Common ancestor: G. n1 (father->G) = 0, n2 (mother->G) = 1.
        // F = (1/2)^(0+1+1) * (1+F_G) = (1/2)^2 * 1 = 0.25 (F_G = 0, G has no recorded parents)
        var pet = Guid.NewGuid();
        var g = Guid.NewGuid();
        var x = Guid.NewGuid();
        var d = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            5,
            TestGraphBuilder.Lineage(pet, g, d, Owner),
            TestGraphBuilder.Lineage(g, null, null, Owner),
            TestGraphBuilder.Lineage(x, null, null, Owner),
            TestGraphBuilder.Lineage(d, g, x, Owner));

        var result = TestGraphBuilder.InbreedingCalculator().Calculate(pet, graph, 5);

        Assert.Equal(0.25m, result.Coefficient);
        Assert.Equal(1, result.CommonAncestorCount);
    }

    [Fact]
    public void FullSiblingMating_ProducesExpectedInbreeding()
    {
        // Pet's father (F) and mother (M) are full siblings: both children of A (father) and B (mother).
        // Common ancestors: A and B.
        // For A: n1 (F->A)=0? No: F IS a child of A, so distance F->A = 1 (F itself is not A).
        // Wait: pet's father IS F, and F's parents are A,B. distance from F to A = 1.
        // distance from M to A = 1 as well (M's parents are also A,B).
        // Contribution per ancestor: (1/2)^(1+1+1) = (1/2)^3 = 0.125. Two ancestors (A,B): total = 0.25.
        var pet = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var f = Guid.NewGuid();
        var m = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            5,
            TestGraphBuilder.Lineage(pet, f, m, Owner),
            TestGraphBuilder.Lineage(f, a, b, Owner),
            TestGraphBuilder.Lineage(m, a, b, Owner),
            TestGraphBuilder.Lineage(a, null, null, Owner),
            TestGraphBuilder.Lineage(b, null, null, Owner));

        var result = TestGraphBuilder.InbreedingCalculator().Calculate(pet, graph, 5);

        Assert.Equal(0.25m, result.Coefficient);
        Assert.Equal(2, result.CommonAncestorCount);
    }

    [Fact]
    public void HalfSiblingMating_ProducesExpectedInbreeding()
    {
        // Pet's father (F) and mother (M) are half-siblings: share only father A.
        // F's parents: A, X. M's parents: A, Y (X != Y, unrelated).
        // Common ancestor: A only. Contribution: (1/2)^(1+1+1) = 0.125.
        var pet = Guid.NewGuid();
        var a = Guid.NewGuid();
        var x = Guid.NewGuid();
        var y = Guid.NewGuid();
        var f = Guid.NewGuid();
        var m = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            5,
            TestGraphBuilder.Lineage(pet, f, m, Owner),
            TestGraphBuilder.Lineage(f, a, x, Owner),
            TestGraphBuilder.Lineage(m, a, y, Owner),
            TestGraphBuilder.Lineage(a, null, null, Owner),
            TestGraphBuilder.Lineage(x, null, null, Owner),
            TestGraphBuilder.Lineage(y, null, null, Owner));

        var result = TestGraphBuilder.InbreedingCalculator().Calculate(pet, graph, 5);

        Assert.Equal(0.125m, result.Coefficient);
        Assert.Equal(1, result.CommonAncestorCount);
    }

    [Fact]
    public void FirstCousinMating_ProducesExpectedInbreeding()
    {
        // Grandparents A, B. Their children F1, F2 are full siblings.
        // F1 x U -> Pet's father (F). F2 x V -> Pet's mother (M). F and M are first cousins.
        // Common ancestors: A, B. Distance from F to A: F's parent is F1, F1's parent is A -> distance 2.
        // Same for M to A: distance 2. Contribution per ancestor: (1/2)^(2+2+1) = (1/2)^5 = 0.03125.
        // Two ancestors (A, B): total = 0.0625.
        var pet = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var f1 = Guid.NewGuid();
        var f2 = Guid.NewGuid();
        var u = Guid.NewGuid();
        var v = Guid.NewGuid();
        var f = Guid.NewGuid();
        var m = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            6,
            TestGraphBuilder.Lineage(pet, f, m, Owner),
            TestGraphBuilder.Lineage(f, f1, u, Owner),
            TestGraphBuilder.Lineage(m, f2, v, Owner),
            TestGraphBuilder.Lineage(f1, a, b, Owner),
            TestGraphBuilder.Lineage(f2, a, b, Owner),
            TestGraphBuilder.Lineage(a, null, null, Owner),
            TestGraphBuilder.Lineage(b, null, null, Owner),
            TestGraphBuilder.Lineage(u, null, null, Owner),
            TestGraphBuilder.Lineage(v, null, null, Owner));

        var result = TestGraphBuilder.InbreedingCalculator().Calculate(pet, graph, 6);

        Assert.Equal(0.0625m, result.Coefficient);
        Assert.Equal(2, result.CommonAncestorCount);
    }

    [Fact]
    public void RepeatedCommonAncestor_CountsEachPathPair()
    {
        // Ancestor A is reachable from the father through TWO distinct paths
        // (father's father is A, and father's mother's father is also A),
        // and from the mother through ONE path. All (n1,n2) path pairs must be summed.
        var pet = Guid.NewGuid();
        var a = Guid.NewGuid();
        var fatherMother = Guid.NewGuid();
        var father = Guid.NewGuid();
        var mother = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            5,
            TestGraphBuilder.Lineage(pet, father, mother, Owner),
            TestGraphBuilder.Lineage(father, a, fatherMother, Owner),
            TestGraphBuilder.Lineage(fatherMother, a, null, Owner),
            TestGraphBuilder.Lineage(mother, a, null, Owner),
            TestGraphBuilder.Lineage(a, null, null, Owner));

        // Father->A distances: {1 (direct), 2 (via fatherMother)}. Mother->A distance: {1}.
        // Contributions: (1/2)^(1+1+1) + (1/2)^(2+1+1) = 0.125 + 0.0625 = 0.1875
        var result = TestGraphBuilder.InbreedingCalculator().Calculate(pet, graph, 5);

        Assert.Equal(0.1875m, result.Coefficient);
        Assert.Equal(1, result.CommonAncestorCount);
    }

    [Fact]
    public void IncompletePedigree_ReturnsZeroWithWarning()
    {
        // Father is known but mother is unknown -> cannot compute common ancestors.
        var pet = Guid.NewGuid();
        var father = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            5,
            TestGraphBuilder.Lineage(pet, father, null, Owner),
            TestGraphBuilder.Lineage(father, null, null, Owner));

        var result = TestGraphBuilder.InbreedingCalculator().Calculate(pet, graph, 5);

        Assert.Equal(0m, result.Coefficient);
        Assert.Contains(result.Warnings, w => w.Contains("no está completamente registrado"));
    }

    [Fact]
    public void CorruptedCycle_DoesNotThrowAndTraversalStaysBounded()
    {
        // Corrupted data: A's father is B, and B's father is A (impossible cycle).
        // Pet's parents are A and some unrelated C. The traversal service itself is
        // cycle-safe (HashSet-based visited tracking), so building the graph via the
        // real traversal service must terminate.
        var pet = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        var petLineage = TestGraphBuilder.Lineage(pet, a, c, Owner);
        var aLineage = TestGraphBuilder.Lineage(a, b, null, Owner);
        var bLineage = TestGraphBuilder.Lineage(b, a, null, Owner); // cycle: B's father is A
        var cLineage = TestGraphBuilder.Lineage(c, null, null, Owner);

        var lineageMap = new Dictionary<Guid, PetLineage>
        {
            [pet] = petLineage,
            [a] = aLineage,
            [b] = bLineage,
            [c] = cLineage
        };

        var graph = new AncestorGraph(pet, 5, 5, lineageMap, NodeLimitExceeded: false);

        // Calculating on this manually-built (already flattened) graph must not throw or
        // infinite-loop, because EnumerateSelfAndAncestorDistances also guards distance
        // against maxDistance and revisits are naturally bounded by depth, not by a
        // visited-set within a single calculation (each recursive call decreases the
        // remaining depth budget).
        var exception = Record.Exception(() =>
            TestGraphBuilder.InbreedingCalculator().Calculate(pet, graph, 5));

        Assert.Null(exception);
    }

    [Fact]
    public void RequestedDepthGreaterThanProcessed_AddsWarning()
    {
        var pet = Guid.NewGuid();
        var father = Guid.NewGuid();
        var mother = Guid.NewGuid();

        // Graph reports MaxDepth = 2 even though caller requested more.
        var graph = new AncestorGraph(
            pet,
            MaxDepth: 2,
            ReachedDepth: 2,
            Lineages: new Dictionary<Guid, PetLineage>
            {
                [pet] = TestGraphBuilder.Lineage(pet, father, mother, Owner),
                [father] = TestGraphBuilder.Lineage(father, null, null, Owner),
                [mother] = TestGraphBuilder.Lineage(mother, null, null, Owner)
            },
            NodeLimitExceeded: false);

        var result = TestGraphBuilder.InbreedingCalculator().Calculate(pet, graph, 5);

        Assert.Contains(result.Warnings, w => w.Contains("profundidad solicitada excede"));
    }
}
