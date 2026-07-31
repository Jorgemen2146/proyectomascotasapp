namespace DogPlatform.Genealogy.Application.Features.GetParents;

public sealed record ParentsResponse(
    Guid PetId,
    Guid? FatherId,
    Guid? MotherId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
