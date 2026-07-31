using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.RemoveFather;

/// <summary>Removes the father relationship for the specified pet.</summary>
public sealed record RemoveFatherCommand(Guid PetId) : IRequest<Result>;
