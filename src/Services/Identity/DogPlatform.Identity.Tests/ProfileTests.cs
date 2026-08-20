using DogPlatform.Identity.Application;
using DogPlatform.Identity.Application.Features.Profile.GetMyProfile;
using DogPlatform.Identity.Application.Features.Profile.UpdateMyProfile;
using DogPlatform.Identity.Domain.Aggregates.User;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;

namespace DogPlatform.Identity.Tests;

public sealed class ProfileTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UpdateProfile_ChangesOnlyAllowedFields()
    {
        var user = CreateUser();
        var originalEmail = user.Email.Value;
        var repository = new FakeUserRepository(user);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateMyProfileCommandHandler(
            repository,
            unitOfWork,
            new TestTimeProvider(UtcNow),
            new UpdateMyProfileCommandValidator());

        var result = await handler.Handle(new UpdateMyProfileCommand(
            user.Id,
            "Jorge",
            "Gonzales",
            "+51987654321"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Jorge", user.FullName.FirstName);
        Assert.Equal("Gonzales", user.FullName.LastName);
        Assert.Equal("+51987654321", user.PhoneNumber);
        Assert.Equal(originalEmail, user.Email.Value);
        Assert.False(user.IsEmailConfirmed);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetProfile_ReturnsPhoneAndEmailConfirmationState()
    {
        var user = CreateUser();
        user.UpdateProfile("+51999999999", null, UtcNow);
        var handler = new GetMyProfileQueryHandler(new FakeUserRepository(user));

        var result = await handler.Handle(
            new GetMyProfileQuery(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.UserId);
        Assert.Equal("ana@example.com", result.Value.Email);
        Assert.Equal("+51999999999", result.Value.PhoneNumber);
        Assert.False(result.Value.IsEmailConfirmed);
    }

    private static User CreateUser()
    {
        return User.Register(
            Guid.NewGuid(),
            FullName.Create("Ana", "Paredes").Value,
            Email.Create("ana@example.com").Value,
            "hash",
            "salt",
            UtcNow);
    }

    private sealed class FakeUserRepository(User user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(id == user.Id ? user : null);

        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(user.Email == email ? user : null);

        public Task AddAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default) =>
            Task.FromResult(user.Email == email);
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
