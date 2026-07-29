using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.Create;

public sealed record CreatePetCommand(
    int BreedId,
    string Name,
    DateTime? BirthDate,
    string Gender,
    decimal? Weight,
    string? Color,
    string? PedigreeNumber,
    bool IsSterilized,
    string? Description) : IRequest<Result<CreatePetResponse>>;
