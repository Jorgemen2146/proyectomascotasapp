namespace DogPlatform.Genealogy.Application.Features.CalculateRelationship;

public enum RelationshipType
{
    SamePet,
    Parent,
    Child,
    FullSibling,
    HalfSibling,
    Grandparent,
    Grandchild,
    UncleOrAunt,
    NephewOrNiece,
    FirstCousin,
    MoreDistantRelative,
    UnrelatedWithinKnownPedigree,
    UnknownDueToIncompletePedigree
}

public sealed record CommonAncestorResponse(
    Guid AncestorPetId,
    int DistanceFromPet1,
    int DistanceFromPet2);

/// <summary>
/// Estimated genealogical relationship between two pets, derived exclusively from
/// recorded parent/child links (paths), not from names or breed similarity. This is an
/// ESTIMATION and does not constitute a veterinary or genetic diagnosis.
/// </summary>
public sealed record RelationshipResponse(
    Guid PetId1,
    Guid PetId2,
    RelationshipType RelationshipType,
    IReadOnlyList<CommonAncestorResponse> CommonAncestors,
    Guid? ClosestCommonAncestor,
    int? MinimumPathLength,
    decimal EstimatedRelationshipCoefficientPercentage,
    bool IsCloseRelative,
    string CalculationMethod,
    IReadOnlyList<string> Warnings);
