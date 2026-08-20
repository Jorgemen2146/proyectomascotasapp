using DogPlatform.Pets.Application.Features.Pets.GetMine;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Domain.Aggregates.Pet;
using DogPlatform.Pets.Domain.Catalog;
using DogPlatform.Pets.Domain.ValueObjects;
using DogPlatform.Pets.Infrastructure.Persistence.Context;
using DogPlatform.Pets.Infrastructure.Persistence.Queries;
using DogPlatform.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Pets.Tests;

public sealed class PetQueryServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("M")]
    public async Task GetMyPets_materializes_converted_gender(string? sex)
    {
        var options = new DbContextOptionsBuilder<PetsDbContext>()
            .UseInMemoryDatabase($"pets-query-{Guid.NewGuid():N}")
            .Options;
        await using var context = new PetsDbContext(options);
        var ownerId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
        context.Species.Add(Species.Create(1, "Perro"));
        context.Breeds.Add(Breed.Create(1, 1, "Mixto"));
        context.Pets.Add(Pet.Create(
            Guid.NewGuid(), ownerId, 1, "Firulais", null, Gender.Create("M").Value,
            null, null, null, false, null, createdAt).Value);
        await context.SaveChangesAsync();
        var service = new PetQueryService(context, new FakePhotoStorage());

        var result = await service.GetMyPetsAsync(
            ownerId,
            new GetMyPetsQuery(PageSize: 100, Sex: sex),
            CancellationToken.None);

        var pet = Assert.Single(result.Items);
        Assert.Equal("M", pet.Sex);
        Assert.Equal("Perro", pet.SpeciesName);
        Assert.Equal("Mixto", pet.BreedName);
    }

    private sealed class FakePhotoStorage : IPhotoStorageService
    {
        public string ProviderName => "Test";
        public Task<Result<PresignedUploadResult>> CreatePresignedUploadAsync(
            Guid userId, Guid petId, string fileName, string contentType, long fileSize,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ObjectExistsAsync(string objectKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<bool> DeleteObjectAsync(string objectKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Result> UploadObjectAsync(
            PhotoUploadRequest request, Stream content, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Result<PhotoContent>> OpenReadAsync(
            string objectKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public string ResolvePublicUrl(string objectKey) => objectKey;
    }
}
