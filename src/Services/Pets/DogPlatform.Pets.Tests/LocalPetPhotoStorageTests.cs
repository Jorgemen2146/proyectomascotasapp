using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Infrastructure.Storage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace DogPlatform.Pets.Tests;

public sealed class LocalPetPhotoStorageTests : IDisposable
{
    private static readonly DateTime UtcNow = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        "DogPlatform.Pets.Tests",
        Guid.NewGuid().ToString("N"));

    [Theory]
    [MemberData(nameof(ValidImages))]
    public async Task SaveAsync_ValidImage_WritesSafePetKey(
        string fileName, string contentType, byte[] bytes)
    {
        var petId = Guid.NewGuid();
        var storage = CreateStorage();

        var result = await storage.SaveAsync(petId, bytes, contentType, fileName);

        Assert.True(result.IsSuccess);
        Assert.StartsWith($"pets/{petId:D}/", result.Value.ObjectKey);
        Assert.DoesNotContain(fileName, result.Value.ObjectKey);
        Assert.True(await storage.ObjectExistsAsync(result.Value.ObjectKey));
    }

    public static TheoryData<string, string, byte[]> ValidImages => new()
    {
        { "luna.jpg", "image/jpeg", [0xFF, 0xD8, 0xFF, 0xD9] },
        { "luna.png", "image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A] },
        { "luna.webp", "image/webp", "RIFF0000WEBP"u8.ToArray() }
    };

    [Theory]
    [InlineData("luna.svg", "image/svg+xml", "Storage.InvalidContentType")]
    [InlineData("luna.exe", "image/jpeg", "Storage.ExtensionMismatch")]
    public async Task SaveAsync_InvalidTypeOrExtension_IsRejected(
        string fileName, string contentType, string expectedCode)
    {
        var result = await CreateStorage().SaveAsync(
            Guid.NewGuid(), [0xFF, 0xD8, 0xFF], contentType, fileName);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task SaveAsync_MismatchedMagicBytes_IsRejected()
    {
        var result = await CreateStorage().SaveAsync(
            Guid.NewGuid(), "<html>"u8.ToArray(), "image/jpeg", "luna.jpg");

        Assert.True(result.IsFailure);
        Assert.Equal("Storage.ContentSignatureMismatch", result.Error.Code);
    }

    [Fact]
    public async Task SaveAsync_DecodedImageOverTenMb_IsRejected()
    {
        var bytes = new byte[10 * 1024 * 1024 + 1];
        bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF;

        var result = await CreateStorage().SaveAsync(
            Guid.NewGuid(), bytes, "image/jpeg", "luna.jpg");

        Assert.True(result.IsFailure);
        Assert.Equal("Storage.FileTooLarge", result.Error.Code);
    }

    [Fact]
    public async Task CreateUploadUrl_ReturnsGatewayPutWithSafeObjectKey()
    {
        var storage = CreateStorage();
        var userId = Guid.NewGuid();
        var petId = Guid.NewGuid();

        var result = await storage.CreatePresignedUploadAsync(
            userId, petId, "../../luna.jpg", "image/jpeg", 4);

        Assert.True(result.IsSuccess);
        Assert.Equal("PUT", result.Value.Method);
        Assert.StartsWith("http://localhost:5101/api/v1/pets/", result.Value.UploadUrl);
        Assert.StartsWith($"pets/{userId:D}/{petId:D}/", result.Value.ObjectKey);
        Assert.DoesNotContain("luna", result.Value.ObjectKey);
        Assert.DoesNotContain("..", result.Value.ObjectKey);
    }

    [Fact]
    public async Task Upload_ValidJpeg_WritesAndCanDeleteObject()
    {
        var storage = CreateStorage();
        var userId = Guid.NewGuid();
        var petId = Guid.NewGuid();
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
        var created = await storage.CreatePresignedUploadAsync(
            userId, petId, "luna.jpg", "image/jpeg", bytes.Length);
        var token = new Uri(created.Value.UploadUrl).Segments.Last();

        var uploaded = await storage.UploadObjectAsync(
            new PhotoUploadRequest(userId, petId, token, "image/jpeg", bytes.Length),
            new MemoryStream(bytes));

        Assert.True(uploaded.IsSuccess);
        Assert.True(await storage.ObjectExistsAsync(created.Value.ObjectKey));
        Assert.True(await storage.DeleteObjectAsync(created.Value.ObjectKey));
        Assert.False(await storage.ObjectExistsAsync(created.Value.ObjectKey));
    }

    [Fact]
    public async Task Upload_WhenContentSignatureDoesNotMatchMime_IsRejected()
    {
        var storage = CreateStorage();
        var userId = Guid.NewGuid();
        var petId = Guid.NewGuid();
        var bytes = "<html>"u8.ToArray();
        var created = await storage.CreatePresignedUploadAsync(
            userId, petId, "luna.jpg", "image/jpeg", bytes.Length);
        var token = new Uri(created.Value.UploadUrl).Segments.Last();

        var result = await storage.UploadObjectAsync(
            new PhotoUploadRequest(userId, petId, token, "image/jpeg", bytes.Length),
            new MemoryStream(bytes));

        Assert.True(result.IsFailure);
        Assert.Equal("Storage.ContentSignatureMismatch", result.Error.Code);
        Assert.False(await storage.ObjectExistsAsync(created.Value.ObjectKey));
    }

    [Fact]
    public async Task CreateUploadUrl_WhenFileIsTooLarge_IsRejected()
    {
        var result = await CreateStorage().CreatePresignedUploadAsync(
            Guid.NewGuid(), Guid.NewGuid(), "large.png", "image/png", 5 * 1024 * 1024 + 1);

        Assert.True(result.IsFailure);
        Assert.Equal("Storage.FileTooLarge", result.Error.Code);
    }

    [Fact]
    public async Task CreateUploadUrl_WhenMimeIsNotAllowed_IsRejected()
    {
        var result = await CreateStorage().CreatePresignedUploadAsync(
            Guid.NewGuid(), Guid.NewGuid(), "image.svg", "image/svg+xml", 100);

        Assert.True(result.IsFailure);
        Assert.Equal("Storage.InvalidContentType", result.Error.Code);
    }

    [Theory]
    [InlineData("../secret.jpg")]
    [InlineData("pets/user/pet/2026/08/../../secret.jpg")]
    [InlineData("C:\\Windows\\system.ini")]
    public async Task PathTraversalObjectKeys_AreRejected(string objectKey)
    {
        var storage = CreateStorage();

        var result = await storage.OpenReadAsync(objectKey);

        Assert.True(result.IsFailure);
        Assert.False(await storage.DeleteObjectAsync(objectKey));
    }

    private LocalPetPhotoStorage CreateStorage()
    {
        var options = Options.Create(new StorageOptions
        {
            Provider = "Local",
            PublicBaseUrl = "http://localhost:5101",
            MaximumFileSizeBytes = 5 * 1024 * 1024,
            Local = new LocalStorageOptions { RootPath = _rootPath }
        });

        return new LocalPetPhotoStorage(
            options,
            new EphemeralDataProtectionProvider(),
            new TestTimeProvider(UtcNow));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
            Directory.Delete(_rootPath, true);
    }

    private sealed class TestTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
