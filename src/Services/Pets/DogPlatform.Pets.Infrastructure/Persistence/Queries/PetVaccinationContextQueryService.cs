using DogPlatform.Pets.Application.Features.Pets.GetVaccinationContexts;
using DogPlatform.Pets.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Pets.Infrastructure.Persistence.Queries;

public sealed class PetVaccinationContextQueryService(PetsDbContext context)
    : IPetVaccinationContextQueryService
{
    public async Task<IReadOnlyCollection<PetVaccinationContextResponse>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await (from pet in context.Pets.AsNoTracking()
               join breed in context.Breeds.AsNoTracking() on pet.BreedId equals breed.Id
               orderby pet.Id
               select new PetVaccinationContextResponse(
                   pet.OwnerId, pet.Id, breed.SpeciesId, pet.BirthDate, pet.Name))
            .ToArrayAsync(cancellationToken);
}
