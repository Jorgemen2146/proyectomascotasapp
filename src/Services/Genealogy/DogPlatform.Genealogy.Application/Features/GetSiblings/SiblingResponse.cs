namespace DogPlatform.Genealogy.Application.Features.GetSiblings;

public enum SiblingRelationship
{
    FullSibling,
    HalfSiblingByFather,
    HalfSiblingByMother
}

/// <summary>A calculated sibling (there is no siblings table; this is derived from shared parents).</summary>
public sealed record SiblingResponse(
    Guid PetId,
    SiblingRelationship Relationship);
