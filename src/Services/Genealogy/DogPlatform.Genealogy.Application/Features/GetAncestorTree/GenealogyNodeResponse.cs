namespace DogPlatform.Genealogy.Application.Features.GetAncestorTree;

/// <summary>
/// A node in the genealogy tree.
/// NOTE: PetsService currently exposes no batch read endpoint (e.g.
/// GET /api/v1/pets/batch?ids=...), so Name/SpeciesId/BreedId/Sex/MainPhotoUrl
/// cannot be populated here without incurring one HTTP call per node (N+1).
/// Until such an endpoint exists, only PetId-based data is returned.
/// </summary>
public sealed record GenealogyNodeResponse(
    Guid PetId,
    string? Name,
    Guid? SpeciesId,
    Guid? BreedId,
    string? Sex,
    string? MainPhotoUrl,
    GenealogyRelationshipType Relationship,
    int Generation,
    GenealogyNodeResponse? Father,
    GenealogyNodeResponse? Mother);

public enum GenealogyRelationshipType
{
    Root,
    Father,
    Mother
}
