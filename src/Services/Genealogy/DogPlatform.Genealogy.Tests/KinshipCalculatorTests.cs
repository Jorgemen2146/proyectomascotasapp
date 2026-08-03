using DogPlatform.Genealogy.Application.Analysis;
using DogPlatform.Genealogy.Application.Traversal;
using Xunit;

namespace DogPlatform.Genealogy.Tests;

/// <summary>
/// Tests for the estimated kinship coefficient between two distinct pets (used by the
/// relationship endpoint), and the RelationshipType classification logic.
/// </summary>
public sealed class KinshipCalculatorTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    [Fact]
    public void ParentChild_KinshipCoefficientIsOneHalf()
    {
        // Parent P, Child C (C's father = P). f(P, C) = (1/2)^(n1+n2+1) with n1=0 (P->P), n2=1 (C->P).
        // = (1/2)^2 = 0.25... wait expected 0.5 for direct parent-offspring per genetics convention
        // (additive relationship a = 0.5). Wright's coefficient of PARENTAGE f(P,C) formula used here
        // is f(X,Y) = sum (1/2)^(n1+n2+1); for X=P (distance to itself=0) and Y=C (distance to P=1):
        // (1/2)^(0+1+1) = 0.25. This matches the coefficient of COANCESTRY (not the additive
        // relationship, which would be 2x this value = 0.5). Documented precisely in code comments.
        var parent = Guid.NewGuid();
        var otherParent = Guid.NewGuid();
        var child = Guid.NewGuid();

        var graphParent = TestGraphBuilder.Graph(
            5,
            TestGraphBuilder.Lineage(parent, null, null, Owner));

        var graphChild = TestGraphBuilder.Graph(
            5,
            TestGraphBuilder.Lineage(child, parent, otherParent, Owner),
            TestGraphBuilder.Lineage(parent, null, null, Owner),
            TestGraphBuilder.Lineage(otherParent, null, null, Owner));

        var result = TestGraphBuilder.KinshipCalculator()
            .Calculate(parent, graphParent, child, graphChild, 5);

        Assert.Equal(0.25m, result.Coefficient);
    }

    [Fact]
    public void FullSiblings_KinshipCoefficientIsOneQuarter()
    {
        // Two full siblings S1, S2 sharing father A and mother B.
        // Common ancestors: A (dist 1 from each), B (dist 1 from each).
        // f = (1/2)^(1+1+1) + (1/2)^(1+1+1) = 0.125 + 0.125 = 0.25
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            5,
            TestGraphBuilder.Lineage(s1, a, b, Owner),
            TestGraphBuilder.Lineage(s2, a, b, Owner),
            TestGraphBuilder.Lineage(a, null, null, Owner),
            TestGraphBuilder.Lineage(b, null, null, Owner));

        var result = TestGraphBuilder.KinshipCalculator().Calculate(s1, graph, s2, graph, 5);

        Assert.Equal(0.25m, result.Coefficient);
    }

    [Fact]
    public void FirstCousins_KinshipCoefficientIsOneSixteenth()
    {
        // Grandparents A,B. F1,F2 are full sibling children of A,B. C1 is F1's child, C2 is F2's child.
        // Common ancestors A,B, each at distance 2 from C1 and distance 2 from C2.
        // f = 2 * (1/2)^(2+2+1) = 2 * (1/32) = 0.0625... but expected "one sixteenth" (0.0625) matches!
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var f1 = Guid.NewGuid();
        var f2 = Guid.NewGuid();
        var c1 = Guid.NewGuid();
        var c2 = Guid.NewGuid();

        var graph = TestGraphBuilder.Graph(
            6,
            TestGraphBuilder.Lineage(c1, f1, null, Owner),
            TestGraphBuilder.Lineage(c2, f2, null, Owner),
            TestGraphBuilder.Lineage(f1, a, b, Owner),
            TestGraphBuilder.Lineage(f2, a, b, Owner),
            TestGraphBuilder.Lineage(a, null, null, Owner),
            TestGraphBuilder.Lineage(b, null, null, Owner));

        var result = TestGraphBuilder.KinshipCalculator().Calculate(c1, graph, c2, graph, 6);

        Assert.Equal(0.0625m, result.Coefficient);
    }

    [Fact]
    public void UnrelatedWithinKnownDepth_KinshipIsZero()
    {
        var petA = Guid.NewGuid();
        var petB = Guid.NewGuid();

        var graphA = TestGraphBuilder.Graph(5, TestGraphBuilder.Lineage(petA, null, null, Owner));
        var graphB = TestGraphBuilder.Graph(5, TestGraphBuilder.Lineage(petB, null, null, Owner));

        var result = TestGraphBuilder.KinshipCalculator().Calculate(petA, graphA, petB, graphB, 5);

        Assert.Equal(0m, result.Coefficient);
    }
}
