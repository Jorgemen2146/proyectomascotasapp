using DogPlatform.Pets.Application.Features.Pets.GetHealthContext;
using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Domain.Aggregates.Pet;
using DogPlatform.Pets.Domain.Catalog;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.Pets.Domain.ValueObjects;

namespace DogPlatform.Pets.Tests;

public sealed class PetHealthContextTests
{
    private static readonly DateTime BirthDate = new(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Owner_receives_minimal_health_context_with_required_fields()
    {
        var ownerId = Guid.NewGuid();
        var pet = CreatePet(ownerId);
        var handler = new GetPetHealthContextQueryHandler(
            new PetRepositoryStub(pet),
            new BreedRepositoryStub(Breed.Create(10, 2, "Domestic")),
            new CurrentUserStub(ownerId));

        var result = await handler.Handle(new(pet.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(pet.Id, result.Value.PetId);
        Assert.Equal(2, result.Value.SpeciesId);
        Assert.Equal(BirthDate, result.Value.BirthDate);
        Assert.Equal("Luna", result.Value.Name);
    }

    [Fact]
    public async Task Missing_pet_returns_not_found()
    {
        var handler = new GetPetHealthContextQueryHandler(
            new PetRepositoryStub(), new BreedRepositoryStub(), new CurrentUserStub(Guid.NewGuid()));
        var result = await handler.Handle(new(Guid.NewGuid()), default);
        Assert.True(result.IsFailure);
        Assert.Equal("Pet.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Non_owner_returns_unauthorized()
    {
        var pet = CreatePet(Guid.NewGuid());
        var handler = new GetPetHealthContextQueryHandler(
            new PetRepositoryStub(pet), new BreedRepositoryStub(Breed.Create(10, 2, "Domestic")),
            new CurrentUserStub(Guid.NewGuid()));
        var result = await handler.Handle(new(pet.Id), default);
        Assert.True(result.IsFailure);
        Assert.Equal("Pet.Unauthorized", result.Error.Code);
    }

    private static Pet CreatePet(Guid ownerId) => Pet.Create(
        Guid.NewGuid(), ownerId, 10, "Luna", BirthDate, Gender.Create("F").Value,
        null, null, null, false, null, DateTime.UtcNow).Value;

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
        public bool IsAuthenticated => true;
    }

    private sealed class PetRepositoryStub(params Pet[] pets) : IPetRepository
    {
        public Task<Pet?> GetByIdAsync(Guid petId, CancellationToken cancellationToken = default) =>
            Task.FromResult(pets.FirstOrDefault(x => x.Id == petId));
        public Task<Pet?> GetByIdWithPhotosAsync(Guid petId, CancellationToken cancellationToken = default) => GetByIdAsync(petId, cancellationToken);
        public Task<IReadOnlyCollection<Pet>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Pet>>(pets.Where(x => x.OwnerId == ownerId).ToArray());
        public Task AddAsync(Pet pet, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Pet pet, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class BreedRepositoryStub(Breed? breed = null) : IBreedRepository
    {
        public Task<Breed?> GetByIdAsync(int breedId, CancellationToken cancellationToken = default) => Task.FromResult(breed);
        public Task<IReadOnlyCollection<Breed>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Breed>>([]);
        public Task<IReadOnlyCollection<Breed>> GetBySpeciesIdAsync(int speciesId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Breed>>([]);
    }
}
