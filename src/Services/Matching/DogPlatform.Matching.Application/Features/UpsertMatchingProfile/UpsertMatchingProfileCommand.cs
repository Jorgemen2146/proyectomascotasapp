using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.UpsertMatchingProfile;

/// <summary>
/// Creates or updates the matching profile for a pet. PetId comes from the route;
/// OwnerId is derived from the JWT — never accepted from the request body.
/// </summary>
public sealed record UpsertMatchingProfileCommand(
    Guid PetId,
    bool IsActive,
    IReadOnlyList<int> PreferredBreedIds,
    int MinimumAgeMonths,
    int MaximumAgeMonths,
    bool RequirePedigree,
    bool RequireGenealogyValidation,
    double MaximumEstimatedInbreedingCoefficient,
    int MinimumCompatibilityScore,
    string? LookingForSex = null,
    bool AllowMixedBreed = true,
    string? Description = null,
    DateTime? AvailableFromUtc = null) : IRequest<Result<MatchingProfileResponse>>;
