namespace DogPlatform.Matching.Application.Features.Matches;

public sealed record PublicMatchingPet(
    Guid PetId,
    string Name,
    string SpeciesName,
    string BreedName,
    string Sex,
    int AgeMonths,
    string? MainPhotoUrl,
    string? Color);

public sealed record SharedOwnerContact(string? DisplayName, string? PhoneNumber);

public sealed record PetMatchSummaryResponse(
    Guid MatchId,
    PublicMatchingPet Pet1,
    PublicMatchingPet Pet2,
    DateTime CreatedAtUtc);

public sealed record PetMatchDetailResponse(
    Guid MatchId,
    string Status,
    PublicMatchingPet Pet1,
    PublicMatchingPet Pet2,
    SharedOwnerContact Pet1Owner,
    SharedOwnerContact Pet2Owner,
    DateTime CreatedAtUtc,
    BreedingIntentSummaryResponse? BreedingIntent);

public sealed record BreedingIntentSummaryResponse(
    Guid BreedingIntentId,
    string Status,
    string? Notes,
    DateTime? ExpectedDateUtc,
    DateTime CreatedAtUtc,
    bool ProposedByCurrentUser);

public sealed record BreedingIntentResponse(
    Guid BreedingIntentId,
    Guid MatchId,
    string Status,
    string? Notes,
    DateTime? ExpectedDateUtc,
    DateTime CreatedAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime? CancelledAtUtc,
    bool ProposedByCurrentUser);
