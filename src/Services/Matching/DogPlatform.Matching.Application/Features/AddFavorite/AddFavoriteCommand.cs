using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.AddFavorite;

public sealed record AddFavoriteCommand(Guid PetId, Guid CandidatePetId) : IRequest<Result>;
