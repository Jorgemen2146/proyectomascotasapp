namespace DogPlatform.Matching.Application.Features.GetFavorites;

public sealed record FavoriteCandidateResponse(
    Guid CandidatePetId,
    string Name,
    int BreedId,
    string BreedName,
    string Sex,
    int AgeMonths,
    string? MainPhotoUrl,
    bool IsAvailable,
    DateTime FavoritedAt);
