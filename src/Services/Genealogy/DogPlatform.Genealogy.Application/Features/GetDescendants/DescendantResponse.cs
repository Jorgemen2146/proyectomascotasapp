namespace DogPlatform.Genealogy.Application.Features.GetDescendants;

public enum DescendantRelationship
{
    Child,
    Grandchild,
    GreatGrandchild,
    Descendant
}

/// <summary>A descendant entry found by walking children generation by generation.</summary>
public sealed record DescendantResponse(
    Guid PetId,
    int Generation,
    DescendantRelationship Relationship,
    IReadOnlyList<string> ParentPaths);
