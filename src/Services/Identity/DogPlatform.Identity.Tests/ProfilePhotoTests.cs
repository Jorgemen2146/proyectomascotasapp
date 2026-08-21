using DogPlatform.Identity.Application;
using DogPlatform.Identity.Application.Features.Profile.Photo;
using DogPlatform.Identity.Application.ProfilePhotos;
using DogPlatform.Identity.Domain.Aggregates.User;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.Identity.Infrastructure.Storage;
using DogPlatform.Logging;
using DogPlatform.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace DogPlatform.Identity.Tests;

public sealed class ProfilePhotoTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"identity-profile-photo-{Guid.NewGuid():N}");

    [Fact]
    public async Task UploadProfilePhoto_StoresLogicalKeyAndDeletesPreviousAfterDatabaseSave()
    {
        var user = CreateUser();
        user.UpdateProfile(null, $"profiles/{user.Id:D}/{Guid.NewGuid():D}.jpg", UtcNow);
        var oldKey = user.ProfilePhotoUrl!;
        var unitOfWork = new FakeUnitOfWork();
        var storage = new FakeStorage(() => unitOfWork.SaveCount == 1);
        var handler = new UploadProfilePhotoCommandHandler(
            new FakeUserRepository(user), unitOfWork, storage, new TestTimeProvider(UtcNow));

        var result = await handler.Handle(new UploadProfilePhotoCommand(
            user.Id, "profile.jpg", "image/jpeg", Convert.ToBase64String([0xFF, 0xD8, 0xFF, 0x01])),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProfilePhotoUrls.Content, result.Value.ProfilePhotoUrl);
        Assert.Equal(storage.StoredKey, user.ProfilePhotoUrl);
        Assert.Equal(oldKey, storage.DeletedKey);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.True(storage.DeleteObservedAfterSave);
    }

    [Fact]
    public async Task UploadProfilePhoto_InvalidBase64_ReturnsValidationWithoutWriting()
    {
        var user = CreateUser();
        var storage = new FakeStorage();
        var handler = new UploadProfilePhotoCommandHandler(
            new FakeUserRepository(user), new FakeUnitOfWork(), storage, new TestTimeProvider(UtcNow));

        var result = await handler.Handle(new UploadProfilePhotoCommand(
            user.Id, "profile.jpg", "image/jpeg", "not-base64!"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Profile.Photo.InvalidBase64", result.Error.Code);
        Assert.Equal(0, storage.SaveCount);
    }

    [Fact]
    public async Task UploadProfilePhoto_DecodedFileOverTenMb_ReturnsValidationWithoutWriting()
    {
        var user = CreateUser();
        var storage = new FakeStorage();
        var handler = new UploadProfilePhotoCommandHandler(
            new FakeUserRepository(user), new FakeUnitOfWork(), storage, new TestTimeProvider(UtcNow));
        var oversized = Convert.ToBase64String(new byte[(10 * 1024 * 1024) + 1]);

        var result = await handler.Handle(new UploadProfilePhotoCommand(
            user.Id, "profile.jpg", "image/jpeg", oversized), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Profile.Photo.InvalidSize", result.Error.Code);
        Assert.Equal(0, storage.SaveCount);
    }

    [Fact]
    public async Task LocalStorage_ValidatesMagicBytesAndUsesGeneratedSafePath()
    {
        var storage = CreateLocalStorage();
        var userId = Guid.NewGuid();

        var invalid = await storage.SaveAsync(
            userId, [0x01, 0x02, 0x03], "image/jpeg", "profile.jpg", CancellationToken.None);
        var valid = await storage.SaveAsync(
            userId, [0xFF, 0xD8, 0xFF, 0x01], "image/jpeg", "ignored.jpg", CancellationToken.None);

        Assert.True(invalid.IsFailure);
        Assert.Equal("Profile.Photo.ContentSignatureMismatch", invalid.Error.Code);
        Assert.True(valid.IsSuccess);
        Assert.StartsWith($"profiles/{userId:D}/", valid.Value.ObjectKey);
        Assert.DoesNotContain("ignored", valid.Value.ObjectKey);
        Assert.True(File.Exists(Path.Combine(
            _rootPath, userId.ToString("D"), Path.GetFileName(valid.Value.ObjectKey))));
    }

    [Fact]
    public void RequestLogging_RemovesImageBase64UsingExistingSanitizer()
    {
        var sanitized = new RequestSanitizer().SanitizeJson(
            "{\"fileName\":\"profile.jpg\",\"imageBase64\":\"sensitive-bytes\"}");

        Assert.Contains("[BASE64_IMAGE_REMOVED]", sanitized);
        Assert.DoesNotContain("sensitive-bytes", sanitized);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
            Directory.Delete(_rootPath, recursive: true);
    }

    private LocalProfilePhotoStorage CreateLocalStorage() => new(Options.Create(new ProfileStorageOptions
    {
        Provider = "Local",
        Local = new LocalProfileStorageOptions { RootPath = _rootPath }
    }));

    private static User CreateUser() => User.Register(
        Guid.NewGuid(),
        FullName.Create("Ana", "Paredes").Value,
        Email.Create("ana@example.com").Value,
        "hash", "salt", UtcNow);

    private sealed class FakeStorage(Func<bool>? databaseSaved = null) : IProfilePhotoStorage
    {
        public string StoredKey { get; } = $"profiles/{Guid.NewGuid():D}/{Guid.NewGuid():D}.jpg";
        public string? DeletedKey { get; private set; }
        public int SaveCount { get; private set; }
        public bool DeleteObservedAfterSave { get; private set; }

        public Task<Result<StoredProfilePhoto>> SaveAsync(
            Guid userId, byte[] content, string contentType, string originalFileName,
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(Result.Success(new StoredProfilePhoto(StoredKey, contentType, content.Length)));
        }

        public Task<Result<ProfilePhotoContent>> OpenReadAsync(
            string objectKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure<ProfilePhotoContent>(Error.NotFound("test", "test")));

        public Task<bool> DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            DeletedKey = objectKey;
            DeleteObservedAfterSave = databaseSaved?.Invoke() ?? true;
            return Task.FromResult(true);
        }
    }

    private sealed class FakeUserRepository(User user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(id == user.Id ? user : null);
        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);
        public Task AddAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class FakeUnitOfWork : IIdentityUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class TestTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
