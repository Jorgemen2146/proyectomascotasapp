using DogPlatform.Matching.Application.Features.UpsertMatchingProfile;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.GetMatchingProfile;

/// <summary>Only the pet's owner can query the full matching profile configuration.</summary>
public sealed record GetMatchingProfileQuery(Guid PetId) : IRequest<Result<MatchingProfileResponse>>;
