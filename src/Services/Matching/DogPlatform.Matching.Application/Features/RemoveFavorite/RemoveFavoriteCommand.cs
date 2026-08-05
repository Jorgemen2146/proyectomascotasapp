using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.RemoveFavorite;

public sealed record RemoveFavoriteCommand(Guid PetId, Guid CandidatePetId) : IRequest<Result>;
