using DogPlatform.Genealogy.Application.Features.CalculateRelationship;
using Xunit;

namespace DogPlatform.Genealogy.Tests;

/// <summary>
/// Tests for RelationshipType classification based purely on generation-distance path
/// pairs to the closest common ancestor (not on names).
/// </summary>
public sealed class RelationshipClassificationTests
{
    [Fact]
    public void ParentChild_ClassifiesAsParent()
    {
        var ancestor = Guid.NewGuid();
        var best = new CommonAncestorResponse(ancestor, DistanceFromPet1: 0, DistanceFromPet2: 1);

        var type = CalculateRelationshipQueryHandler.ClassifyRelationship(best, new[] { best });

        Assert.Equal(RelationshipType.Parent, type);
    }

    [Fact]
    public void ChildParent_ClassifiesAsChild()
    {
        var ancestor = Guid.NewGuid();
        var best = new CommonAncestorResponse(ancestor, DistanceFromPet1: 1, DistanceFromPet2: 0);

        var type = CalculateRelationshipQueryHandler.ClassifyRelationship(best, new[] { best });

        Assert.Equal(RelationshipType.Child, type);
    }

    [Fact]
    public void TwoSharedParents_ClassifiesAsFullSibling()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var commonA = new CommonAncestorResponse(a, 1, 1);
        var commonB = new CommonAncestorResponse(b, 1, 1);

        var type = CalculateRelationshipQueryHandler.ClassifyRelationship(commonA, new[] { commonA, commonB });

        Assert.Equal(RelationshipType.FullSibling, type);
    }

    [Fact]
    public void OneSharedParent_ClassifiesAsHalfSibling()
    {
        var a = Guid.NewGuid();
        var commonA = new CommonAncestorResponse(a, 1, 1);

        var type = CalculateRelationshipQueryHandler.ClassifyRelationship(commonA, new[] { commonA });

        Assert.Equal(RelationshipType.HalfSibling, type);
    }

    [Fact]
    public void FirstCousinDistances_ClassifiesAsFirstCousin()
    {
        var a = Guid.NewGuid();
        var common = new CommonAncestorResponse(a, 2, 2);

        var type = CalculateRelationshipQueryHandler.ClassifyRelationship(common, new[] { common });

        Assert.Equal(RelationshipType.FirstCousin, type);
    }

    [Fact]
    public void DistantDistances_ClassifiesAsMoreDistantRelative()
    {
        var a = Guid.NewGuid();
        var common = new CommonAncestorResponse(a, 3, 4);

        var type = CalculateRelationshipQueryHandler.ClassifyRelationship(common, new[] { common });

        Assert.Equal(RelationshipType.MoreDistantRelative, type);
    }
}
