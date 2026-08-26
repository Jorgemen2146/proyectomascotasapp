using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.DeactivateMatchingProfile;

public sealed record DeactivateMatchingProfileCommand(Guid MatchingProfileId) : IRequest<Result>;
