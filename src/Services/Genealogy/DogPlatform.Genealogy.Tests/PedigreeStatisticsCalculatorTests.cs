using DogPlatform.Genealogy.Application.Analysis;
using DogPlatform.Genealogy.Application.Traversal;
using Xunit;

namespace DogPlatform.Genealogy.Tests;

/// <summary>
/// Tests for pedigree completeness percentage, generation distribution, and repeated
/// ancestor detection.
/// </summary>
public sealed class PedigreeStatisticsCalculatorTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    private static IPedigreeStatisticsCalculator Calculator() =>
        new PedigreeStatisticsCalculator(TestGraphBuilder.Traversal(), TestGraphBuilder.InbreedingCalculator());

    [Fact]
    public void EmptyPedigree_CompletenessIsZeroPercent()
    {
        // No parents at all recorded for depth 1: 2 expected positions, 0 known.
        var pet = Guid.NewGuid();
        var graph = TestGraphBuilder.Graph(1, TestGraphBuilder.Lineage(pet, null, null, Owner));

        var result = Calculator().Calculate(pet, graph, 1);

        Assert.Equal(2, result.TotalPositions);
        Assert.Equal(0, result.KnownAncestorPositions);
        Assert.Equal(0m, result.PedigreeCompletenessPercentage);
    }

    [Fact]
    public void OneOfTwoParentsKnown_CompletenessIsFiftyPercent()
    {
        var pet = Guid.NewGuid();
        var father = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            1,
            TestGraphBuilder.Lineage(pet, father, null, Owner),
            TestGraphBuilder.Lineage(father, null, null, Owner));

        var result = Calculator().Calculate(pet, graph, 1);

        Assert.Equal(2, result.TotalPositions);
        Assert.Equal(1, result.KnownAncestorPositions);
        Assert.Equal(50m, result.PedigreeCompletenessPercentage);
    }

    [Fact]
    public void BothParentsKnown_CompletenessIsOneHundredPercent()
    {
        var pet = Guid.NewGuid();
        var father = Guid.NewGuid();
        var mother = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            1,
            TestGraphBuilder.Lineage(pet, father, mother, Owner),
            TestGraphBuilder.Lineage(father, null, null, Owner),
            TestGraphBuilder.Lineage(mother, null, null, Owner));

        var result = Calculator().Calculate(pet, graph, 1);

        Assert.Equal(2, result.TotalPositions);
        Assert.Equal(2, result.KnownAncestorPositions);
        Assert.Equal(100m, result.PedigreeCompletenessPercentage);
    }

    [Fact]
    public void RepeatedAncestor_IsCountedAsTwoPositionsAndOneUniqueAncestor()
    {
        // Ancestor A appears as both paternal grandfather and maternal grandfather.
        var pet = Guid.NewGuid();
        var father = Guid.NewGuid();
        var mother = Guid.NewGuid();
        var a = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            2,
            TestGraphBuilder.Lineage(pet, father, mother, Owner),
            TestGraphBuilder.Lineage(father, a, null, Owner),
            TestGraphBuilder.Lineage(mother, a, null, Owner),
            TestGraphBuilder.Lineage(a, null, null, Owner));

        var result = Calculator().Calculate(pet, graph, 2);

        // Generation 2 has 4 expected positions (paternal grandfather/grandmother,
        // maternal grandfather/grandmother); A occupies 2 of them. Unique ancestors across
        // all known positions (father, mother, A) = 3.
        Assert.Equal(3, result.UniqueAncestorCount);
        Assert.Equal(1, result.RepeatedAncestorCount);
        Assert.Single(result.RepeatedAncestors);
        Assert.Equal(2, result.RepeatedAncestors[0].OccurrenceCount);
        Assert.Equal(a, result.RepeatedAncestors[0].AncestorPetId);
    }

    [Fact]
    public void GenerationDistribution_ReportsExpectedPositionsPerGeneration()
    {
        var pet = Guid.NewGuid();
        var father = Guid.NewGuid();
        var mother = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            2,
            TestGraphBuilder.Lineage(pet, father, mother, Owner),
            TestGraphBuilder.Lineage(father, null, null, Owner),
            TestGraphBuilder.Lineage(mother, null, null, Owner));

        var result = Calculator().Calculate(pet, graph, 2);

        Assert.Equal(2, result.AncestorsByGeneration.Count);
        Assert.Equal(2, result.AncestorsByGeneration[0].ExpectedPositions); // generation 1
        Assert.Equal(4, result.AncestorsByGeneration[1].ExpectedPositions); // generation 2
    }
}
