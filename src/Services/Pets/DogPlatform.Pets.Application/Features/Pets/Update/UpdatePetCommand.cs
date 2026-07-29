using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.Update;

public sealed record UpdatePetCommand(
    Guid PetId,
    string Name,
    DateTime? BirthDate,
    string Gender,
    decimal? Weight,
    string? Color,
    string? PedigreeNumber,
    bool IsSterilized,
    string? Description) : IRequest<Result<UpdatePetResponse>>;
