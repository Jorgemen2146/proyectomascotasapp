using DogPlatform.Pets.Domain.Aggregates.Pet;
using DogPlatform.Pets.Domain.Catalog;
using DogPlatform.Pets.Domain.ValueObjects;
using DogPlatform.Pets.Infrastructure.Persistence.Context;
using DogPlatform.Pets.Infrastructure.Persistence.Queries;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Pets.Tests;

public sealed class PetVaccinationContextQueryServiceTests
{
    [Fact]
    public async Task InternalVaccinationContext_ReturnsRealOwnerUserId()
    {
        var options = new DbContextOptionsBuilder<PetsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new PetsDbContext(options);
        var ownerUserId = Guid.NewGuid();
        var petId = Guid.NewGuid();
        var birthDate = new DateTime(2020, 1, 2);

        context.Species.Add(Species.Create(2, "Cat"));
        context.Breeds.Add(Breed.Create(33, 2, "Bengal"));
        context.Pets.Add(Pet.Create(
            petId, ownerUserId, 33, "Sandra", birthDate, Gender.Create("F").Value,
            null, null, null, false, null, DateTime.UtcNow).Value);
        await context.SaveChangesAsync();

        var result = await new PetVaccinationContextQueryService(context).GetAllAsync();

        var candidate = Assert.Single(result);
        Assert.Equal(ownerUserId, candidate.UserId);
        Assert.Equal(petId, candidate.PetId);
        Assert.Equal(2, candidate.SpeciesId);
        Assert.Equal(birthDate, candidate.BirthDate);
        Assert.Equal("Sandra", candidate.PetName);
    }
}
