namespace DogPlatform.Matching.Domain.Enums;

/// <summary>
/// Snapshot of the relationship type returned by GenealogyService at the moment
/// a candidate was evaluated or a request was created. Mirrors Genealogy's
/// RelationshipType values; Matching does not recalculate this itself.
/// </summary>
public enum RelationshipTypeSnapshot
{
    SamePet = 0,
    Parent = 1,
    Child = 2,
    FullSibling = 3,
    HalfSibling = 4,
    Grandparent = 5,
    Grandchild = 6,
    UncleOrAunt = 7,
    NephewOrNiece = 8,
    FirstCousin = 9,
    MoreDistantRelative = 10,
    UnrelatedWithinKnownPedigree = 11,
    UnknownDueToIncompletePedigree = 12
}
