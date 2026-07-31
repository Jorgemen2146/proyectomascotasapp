using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Genealogy.Application.Features.RemoveMother;

/// <summary>Removes the mother relationship for the specified pet.</summary>
public sealed record RemoveMotherCommand(Guid PetId) : IRequest<Result>;
