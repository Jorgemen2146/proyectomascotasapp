using DogPlatform.Pets.Application.Common;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.GetMine;

public sealed record GetMyPetsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Name = null,
    int? SpeciesId = null,
    int? BreedId = null,
    string? Sex = null,
    string SortBy = "CreatedAt",
    string SortDirection = "DESC")
    : IRequest<Result<PagedResult<MyPetResponse>>>;

