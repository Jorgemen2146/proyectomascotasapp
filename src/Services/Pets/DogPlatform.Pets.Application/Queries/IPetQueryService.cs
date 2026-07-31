using DogPlatform.Pets.Application.Common;
using DogPlatform.Pets.Application.Features.Pets.GetMine;

namespace DogPlatform.Pets.Application.Queries;

/// <summary>
/// Read-only query service for pet list operations.
/// Kept separate from IPetRepository, which handles aggregate write operations.
/// </summary>
public interface IPetQueryService
{
    Task<PagedResult<MyPetResponse>> GetMyPetsAsync(
        Guid ownerId,
        GetMyPetsQuery query,
        CancellationToken cancellationToken = default);
}
