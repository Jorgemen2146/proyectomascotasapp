using DogPlatform.Pets.Application;
using DogPlatform.Pets.Application.Common;
using DogPlatform.Pets.Application.Features.PetPhotos.ConfirmUpload;
using DogPlatform.Pets.Application.Features.PetPhotos.CreateUploadUrl;
using DogPlatform.Pets.Application.Features.PetPhotos.Delete;
using DogPlatform.Pets.Application.Features.Pets.Create;
using DogPlatform.Pets.Application.Features.Pets.GetMine;
using DogPlatform.Pets.Application.Features.Pets.Update;
using DogPlatform.Pets.Application.Queries;
using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Domain.Aggregates.Pet;
using DogPlatform.Pets.Domain.Catalog;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.Pets.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;
using Microsoft.Extensions.Logging.Abstractions;

namespace DogPlatform.Pets.Tests;

public sealed class PetWorkflowTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreatePet_AddsPetForCurrentUser()
    {
        var ownerId = Guid.NewGuid();
        var pets = new FakePetRepository();
        var handler = new CreatePetCommandHandler(
            pets,
            new FakeBreedRepository(),
            new FakeUnitOfWork(),
            new FakeCurrentUser(ownerId),
            new TestTimeProvider(UtcNow));

        var result = await handler.Handle(new CreatePetCommand(
            1, "Luna", UtcNow.AddYears(-2), "F", 12.5m, "Black", null, true, "Friendly"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(pets.Pets);
        Assert.Equal(ownerId, pets.Pets[0].OwnerId);
    }

    [Fact]
    public async Task UpdatePet_ChangesEditableFields()
    {
        var ownerId = Guid.NewGuid();
        var pet = CreatePet(ownerId);
        var pets = new FakePetRepository(pet);
        var handler = new UpdatePetCommandHandler(
            pets,
            new FakeUnitOfWork(),
            new FakeCurrentUser(ownerId),
            new TestTimeProvider(UtcNow));

        var result = await handler.Handle(new UpdatePetCommand(
            pet.Id, "Luna II", null, "F", 13m, "Brown", null, true, "Updated"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Luna II", pet.Name);
        Assert.Equal(13m, pet.Weight);
    }

    [Fact]
    public async Task GetMyPets_UsesAuthenticatedOwner()
    {
        var ownerId = Guid.NewGuid();
        var queryService = new FakePetQueryService();
        var handler = new GetMyPetsQueryHandler(
            queryService,
            new FakeCurrentUser(ownerId),
            new GetMyPetsQueryValidator());

        var result = await handler.Handle(new GetMyPetsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ownerId, queryService.RequestedOwnerId);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task CreateUploadUrl_ForOwner_ReturnsProviderNeutralPutContract()
    {
        var ownerId = Guid.NewGuid();
        var pet = CreatePet(ownerId);
        var storage = new FakePhotoStorage();
        var handler = new CreatePetPhotoUploadUrlCommandHandler(
            new FakePetRepository(pet), storage, new FakeCurrentUser(ownerId));

        var result = await handler.Handle(new CreatePetPhotoUploadUrlCommand(
            pet.Id, "luna.jpg", "image/jpeg", 100), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("PUT", result.Value.Method);
        Assert.True(storage.CreateCalled);
    }

    [Fact]
    public async Task CreateUploadUrl_ForNonOwner_IsForbiddenBeforeStorageCall()
    {
        var pet = CreatePet(Guid.NewGuid());
        var storage = new FakePhotoStorage();
        var handler = new CreatePetPhotoUploadUrlCommandHandler(
            new FakePetRepository(pet), storage, new FakeCurrentUser(Guid.NewGuid()));

        var result = await handler.Handle(new CreatePetPhotoUploadUrlCommand(
            pet.Id, "luna.jpg", "image/jpeg", 100), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Pet.Unauthorized", result.Error.Code);
        Assert.False(storage.CreateCalled);
    }

    [Fact]
    public async Task ConfirmUpload_WhenObjectExists_AddsMetadataAndMakesFirstPhotoMain()
    {
        var ownerId = Guid.NewGuid();
        var pet = CreatePet(ownerId);
        var photos = new FakePhotoRepository();
        var storage = new FakePhotoStorage { Exists = true };
        var objectKey = $"pets/{ownerId:D}/{pet.Id:D}/2026/08/{Guid.NewGuid():D}.jpg";
        var handler = new ConfirmPetPhotoUploadCommandHandler(
            new FakePetRepository(pet),
            photos,
            storage,
            new FakeUnitOfWork(),
            new FakeCurrentUser(ownerId),
            new TestTimeProvider(UtcNow));

        var result = await handler.Handle(
            new ConfirmPetPhotoUploadCommand(pet.Id, objectKey),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsMain);
        Assert.Single(photos.Photos);
        Assert.StartsWith("http://localhost:5101/", result.Value.Url);
    }

    [Fact]
    public async Task DeletePhoto_RemovesMetadataAndStorageObject()
    {
        var ownerId = Guid.NewGuid();
        var pet = CreatePet(ownerId);
        var objectKey = $"pets/{ownerId:D}/{pet.Id:D}/2026/08/{Guid.NewGuid():D}.jpg";
        var photo = PetPhoto.Create(Guid.NewGuid(), pet.Id, objectKey, true, UtcNow);
        pet.AddPhoto(photo);
        var photos = new FakePhotoRepository(photo);
        var storage = new FakePhotoStorage();
        var handler = new DeletePetPhotoCommandHandler(
            new FakePetRepository(pet),
            photos,
            storage,
            new FakeUnitOfWork(),
            new FakeCurrentUser(ownerId),
            NullLogger<DeletePetPhotoCommandHandler>.Instance);

        var result = await handler.Handle(
            new DeletePetPhotoCommand(pet.Id, photo.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(photos.Photos);
        Assert.Equal(objectKey, storage.DeletedObjectKey);
    }

    private static Pet CreatePet(Guid ownerId) => Pet.Create(
        Guid.NewGuid(), ownerId, 1, "Luna", null, Gender.Create("F").Value,
        12m, "Black", null, false, null, UtcNow).Value;

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
        public bool IsAuthenticated => true;
    }

    private sealed class TestTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class FakeUnitOfWork : IPetsUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeBreedRepository : IBreedRepository
    {
        public Task<Breed?> GetByIdAsync(int breedId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Breed?>(Breed.Create(breedId, 1, "Mixed Breed"));
        public Task<IReadOnlyCollection<Breed>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Breed>>([]);
        public Task<IReadOnlyCollection<Breed>> GetBySpeciesIdAsync(int speciesId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Breed>>([]);
    }

    private sealed class FakePetRepository(params Pet[] pets) : IPetRepository
    {
        public List<Pet> Pets { get; } = [.. pets];
        public Task<Pet?> GetByIdAsync(Guid petId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Pets.FirstOrDefault(p => p.Id == petId));
        public Task<Pet?> GetByIdWithPhotosAsync(Guid petId, CancellationToken cancellationToken = default) =>
            GetByIdAsync(petId, cancellationToken);
        public Task<IReadOnlyCollection<Pet>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Pet>>(Pets.Where(p => p.OwnerId == ownerId).ToList());
        public Task AddAsync(Pet pet, CancellationToken cancellationToken = default)
        {
            Pets.Add(pet);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(Pet pet, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakePhotoRepository(params PetPhoto[] photos) : IPetPhotoRepository
    {
        public List<PetPhoto> Photos { get; } = [.. photos];
        public Task<IReadOnlyCollection<PetPhoto>> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<PetPhoto>>(Photos.Where(p => p.PetId == petId).ToList());
        public Task<PetPhoto?> GetByIdAsync(Guid photoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Photos.FirstOrDefault(p => p.Id == photoId));
        public Task AddAsync(PetPhoto photo, CancellationToken cancellationToken = default)
        {
            Photos.Add(photo);
            return Task.CompletedTask;
        }
        public Task RemoveAsync(PetPhoto photo, CancellationToken cancellationToken = default)
        {
            Photos.Remove(photo);
            return Task.CompletedTask;
        }
        public Task<bool> ExistsByUrlAsync(Guid petId, string url, CancellationToken cancellationToken = default) =>
            Task.FromResult(Photos.Any(p => p.PetId == petId && p.Url == url));
    }

    private sealed class FakePhotoStorage : IPhotoStorageService
    {
        public string ProviderName => "Local";
        public bool CreateCalled { get; private set; }
        public bool Exists { get; init; }
        public string? DeletedObjectKey { get; private set; }

        public Task<Result<PresignedUploadResult>> CreatePresignedUploadAsync(
            Guid userId, Guid petId, string fileName, string contentType, long fileSize,
            CancellationToken cancellationToken = default)
        {
            CreateCalled = true;
            return Task.FromResult(Result.Success(new PresignedUploadResult(
                $"pets/{userId:D}/{petId:D}/2026/08/{Guid.NewGuid():D}.jpg",
                $"http://localhost:5101/api/v1/pets/{petId:D}/photos/upload/token",
                "PUT", UtcNow.AddMinutes(10), new Dictionary<string, string>())));
        }

        public Task<bool> ObjectExistsAsync(string objectKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Exists);
        public Task<bool> DeleteObjectAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            DeletedObjectKey = objectKey;
            return Task.FromResult(true);
        }
        public Task<Result> UploadObjectAsync(PhotoUploadRequest request, Stream content, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
        public Task<Result<PhotoContent>> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure<PhotoContent>(Error.NotFound("Storage.NotFound", "Not found")));
        public string ResolvePublicUrl(string objectKey) => $"http://localhost:5101/content/{objectKey}";
    }

    private sealed class FakePetQueryService : IPetQueryService
    {
        public Guid RequestedOwnerId { get; private set; }
        public Task<PagedResult<MyPetResponse>> GetMyPetsAsync(
            Guid ownerId,
            GetMyPetsQuery query,
            CancellationToken cancellationToken = default)
        {
            RequestedOwnerId = ownerId;
            IReadOnlyCollection<MyPetResponse> items =
            [
                new(Guid.NewGuid(), "Luna", 1, "Dog", 1, "Mixed Breed", "F", null, null, UtcNow, null)
            ];
            return Task.FromResult(PagedResult<MyPetResponse>.Create(items, 1, query.PageNumber, query.PageSize));
        }
    }
}
