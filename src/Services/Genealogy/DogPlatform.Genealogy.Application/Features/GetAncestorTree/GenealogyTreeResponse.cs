namespace DogPlatform.Genealogy.Application.Features.GetAncestorTree;

public sealed record GenealogyTreeResponse(
    Guid RootPetId,
    int Depth,
    GenealogyNodeResponse Root);
