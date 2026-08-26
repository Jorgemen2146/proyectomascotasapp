using DogPlatform.Identity.Application;
using DogPlatform.Identity.Application.Communication;
using DogPlatform.Identity.Application.Features.Authentication.PasswordReset;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Aggregates.PasswordResetCode;
using DogPlatform.Identity.Domain.Aggregates.RefreshToken;
using DogPlatform.Identity.Domain.Aggregates.User;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DogPlatform.Identity.Tests;

public sealed class PasswordResetTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);
    private const string EmailAddress = "user@example.com";
    private const string ResetCode = "483921";

    [Fact]
    public async Task Forgot_ExistingEmail_CreatesHashedCodeAndSendsEmail()
    {
        var fixture = new Fixture();

        var result = await fixture.Forgot(EmailAddress);

        Assert.True(result.IsSuccess);
        Assert.Equal(ForgotPasswordCommandHandler.GenericMessage, result.Value.Message);
        var stored = Assert.Single(fixture.Codes.Items);
        Assert.NotEqual(ResetCode, stored.CodeHash);
        Assert.Equal("password-reset-hash", stored.CodeHash);
        Assert.Equal(UtcNow.AddMinutes(10), stored.ExpiresAtUtc);
        Assert.Equal((EmailAddress, ResetCode, 10), Assert.Single(fixture.Email.PasswordResetMessages));
    }

    [Fact]
    public async Task Forgot_UnknownEmail_ReturnsExactlySameGenericResponse()
    {
        var existing = new Fixture();
        var missing = new Fixture(userExists: false);

        var existingResult = await existing.Forgot(EmailAddress);
        var missingResult = await missing.Forgot("missing@example.com");

        Assert.Equal(existingResult.Value, missingResult.Value);
        Assert.Empty(missing.Codes.Items);
        Assert.Empty(missing.Email.PasswordResetMessages);
    }

    [Fact]
    public async Task Forgot_WithinCooldown_DoesNotIssueAnotherCode()
    {
        var fixture = new Fixture();
        await fixture.Forgot(EmailAddress);
        fixture.Time.Advance(TimeSpan.FromSeconds(30));

        var result = await fixture.Forgot(EmailAddress);

        Assert.True(result.IsSuccess);
        Assert.Single(fixture.Codes.Items);
        Assert.Single(fixture.Email.PasswordResetMessages);
    }

    [Fact]
    public async Task Forgot_AfterCooldown_RevokesPreviousCode()
    {
        var fixture = new Fixture();
        await fixture.Forgot(EmailAddress);
        var previous = Assert.Single(fixture.Codes.Items);
        fixture.Time.Advance(TimeSpan.FromSeconds(61));

        await fixture.Forgot(EmailAddress);

        Assert.True(previous.IsRevoked);
        Assert.Equal(2, fixture.Codes.Items.Count);
        Assert.Equal(2, fixture.Email.PasswordResetMessages.Count);
    }

    [Fact]
    public async Task Verify_CorrectCode_IsValidWithoutConsumingIt()
    {
        var fixture = new Fixture();
        var code = fixture.AddCode();

        var result = await fixture.Verify(ResetCode);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Valid);
        Assert.Null(code.UsedAtUtc);
    }

    [Fact]
    public async Task Verify_ExpiredCode_IsRejectedAndRevoked()
    {
        var fixture = new Fixture();
        var code = fixture.AddCode(expiresAtUtc: UtcNow.AddSeconds(-1));

        var result = await fixture.Verify(ResetCode);

        Assert.Equal("PASSWORD_RESET_CODE_EXPIRED", result.Error.Code);
        Assert.True(code.IsRevoked);
    }

    [Fact]
    public async Task Verify_IncorrectCode_IncrementsFailedAttempts()
    {
        var fixture = new Fixture();
        var code = fixture.AddCode();

        var result = await fixture.Verify("000000");

        Assert.Equal("PASSWORD_RESET_CODE_INVALID", result.Error.Code);
        Assert.Equal(1, code.FailedAttempts);
        Assert.False(code.IsRevoked);
    }

    [Fact]
    public async Task Verify_MaximumAttempts_LocksCodeWithoutLockingUser()
    {
        var fixture = new Fixture();
        var code = fixture.AddCode();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var result = await fixture.Verify("000000");
            Assert.Equal(attempt == 5
                ? "PASSWORD_RESET_CODE_LOCKED"
                : "PASSWORD_RESET_CODE_INVALID", result.Error.Code);
        }

        Assert.True(code.IsRevoked);
        Assert.True(fixture.User.IsActive);
    }

    [Fact]
    public async Task Reset_CorrectCode_ChangesPasswordConsumesCodeAndRevokesRefreshTokens()
    {
        var fixture = new Fixture();
        var code = fixture.AddCode();
        var refreshToken = fixture.AddRefreshToken();

        var result = await fixture.Reset(ResetCode, "NewPassword1", "NewPassword1");

        Assert.True(result.IsSuccess);
        Assert.Equal("new-password-hash", fixture.User.PasswordHash);
        Assert.Equal("new-password-salt", fixture.User.PasswordSalt);
        Assert.Equal(UtcNow, code.UsedAtUtc);
        Assert.True(refreshToken.IsRevoked);
    }

    [Fact]
    public async Task Reset_UsedCode_CannotBeReused()
    {
        var fixture = new Fixture();
        fixture.AddCode();
        Assert.True((await fixture.Reset(ResetCode, "NewPassword1", "NewPassword1")).IsSuccess);

        var reused = await fixture.Reset(ResetCode, "OtherPassword2", "OtherPassword2");

        Assert.Equal("PASSWORD_RESET_CODE_INVALID", reused.Error.Code);
    }

    [Theory]
    [InlineData("short", "short")]
    [InlineData("alllowercase1", "alllowercase1")]
    [InlineData("ALLUPPERCASE1", "ALLUPPERCASE1")]
    [InlineData("NoNumberPassword", "NoNumberPassword")]
    [InlineData("ValidPassword1", "DifferentPassword1")]
    public async Task Reset_UsesRegisterPasswordPolicyAndConfirmation(
        string password, string confirmation)
    {
        var fixture = new Fixture();
        fixture.AddCode();

        var result = await fixture.Reset(ResetCode, password, confirmation);

        Assert.Equal("PASSWORD_RESET_PASSWORD_INVALID", result.Error.Code);
        Assert.Null(fixture.Codes.Items.Single().UsedAtUtc);
    }

    private sealed class Fixture
    {
        private readonly FakeUserRepository _users;
        private readonly PasswordResetOptions _options = new();

        public Fixture(bool userExists = true)
        {
            User = CreateUser();
            _users = new FakeUserRepository(userExists ? User : null);
        }

        public User User { get; }
        public FakePasswordResetCodeRepository Codes { get; } = new();
        public FakeEmailSender Email { get; } = new();
        public FakeRefreshTokenRepository RefreshTokens { get; } = new();
        public MutableTimeProvider Time { get; } = new(UtcNow);
        public FakeUnitOfWork UnitOfWork { get; } = new();

        public Task<DogPlatform.SharedKernel.Primitives.Result<ForgotPasswordResponse>> Forgot(string email) =>
            new ForgotPasswordCommandHandler(
                _users, Codes, new FakeCodeService(), Email, UnitOfWork, Time,
                Options.Create(_options), new ForgotPasswordValidator(),
                NullLogger<ForgotPasswordCommandHandler>.Instance)
            .Handle(new ForgotPasswordCommand(email, "127.0.0.1"), default);

        public Task<DogPlatform.SharedKernel.Primitives.Result<VerifyResetCodeResponse>> Verify(string code) =>
            new VerifyResetCodeCommandHandler(
                _users, Codes, new FakeCodeService(), UnitOfWork, Time,
                Options.Create(_options), new VerifyResetCodeValidator())
            .Handle(new VerifyResetCodeCommand(EmailAddress, code), default);

        public Task<DogPlatform.SharedKernel.Primitives.Result> Reset(
            string code, string password, string confirmation) =>
            new ResetPasswordCommandHandler(
                _users, Codes, RefreshTokens, new FakeCodeService(),
                new FakePasswordHasher(), UnitOfWork, Time, Options.Create(_options),
                new ResetPasswordValidator())
            .Handle(new ResetPasswordCommand(
                EmailAddress, code, password, confirmation), default);

        public PasswordResetCode AddCode(DateTime? expiresAtUtc = null)
        {
            var createdAt = expiresAtUtc.HasValue && expiresAtUtc <= UtcNow
                ? expiresAtUtc.Value.AddMinutes(-10)
                : UtcNow;
            var code = PasswordResetCode.Create(
                Guid.NewGuid(), User.Id, "password-reset-hash", createdAt,
                expiresAtUtc ?? UtcNow.AddMinutes(10));
            Codes.Items.Add(code);
            return code;
        }

        public RefreshToken AddRefreshToken()
        {
            var token = RefreshToken.Create(
                Guid.NewGuid(), User.Id, Guid.NewGuid().ToString("N"),
                UtcNow.AddDays(1), UtcNow);
            RefreshTokens.Items.Add(token);
            return token;
        }
    }

    private static User CreateUser() => User.Register(
        Guid.NewGuid(), FullName.Create("Test", "User").Value,
        Email.Create(EmailAddress).Value, "old-hash", "old-salt", UtcNow);

    private sealed class FakeCodeService : IPasswordResetCodeService
    {
        public PasswordResetCodeResult Generate() => new(ResetCode, "password-reset-hash");
        public bool Verify(string code, string expectedHash) =>
            code == ResetCode && expectedHash == "password-reset-hash";
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<(string Email, string Code, int ExpirationMinutes)> PasswordResetMessages { get; } = [];
        public Task SendVerificationCodeAsync(string email, string code,
            CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendPasswordResetCodeAsync(string email, string code,
            int expirationMinutes, CancellationToken cancellationToken)
        {
            PasswordResetMessages.Add((email, code, expirationMinutes));
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordHashResult Hash(string password) =>
            new("new-password-hash", "new-password-salt");
        public bool Verify(string password, string hash, string salt) => false;
    }

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(user?.Id == id ? user : null);
        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
            Task.FromResult(user?.Email.Value == email.Value ? user : null);
        public Task AddAsync(User newUser, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(User updatedUser, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default) =>
            Task.FromResult(user?.Email.Value == email.Value);
    }

    private sealed class FakePasswordResetCodeRepository : IPasswordResetCodeRepository
    {
        public List<PasswordResetCode> Items { get; } = [];
        public Task<PasswordResetCode?> GetLatestByUserIdAsync(
            Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(code => code.UserId == userId)
                .OrderByDescending(code => code.CreatedAtUtc).ThenByDescending(code => code.Id)
                .FirstOrDefault());
        public Task<IReadOnlyCollection<PasswordResetCode>> GetPendingByUserIdAsync(
            Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<PasswordResetCode>>(Items
                .Where(code => code.UserId == userId && code.UsedAtUtc is null && !code.IsRevoked)
                .ToList());
        public Task AddAsync(PasswordResetCode code, CancellationToken cancellationToken = default)
        {
            Items.Add(code);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> Items { get; } = [];
        public Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(token => token.Id == id));
        public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(item => item.Token == token));
        public Task<IReadOnlyCollection<RefreshToken>> GetActiveByUserIdAsync(
            Guid userId, DateTime utcNow, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<RefreshToken>>(Items
                .Where(token => token.UserId == userId && token.IsActive(utcNow)).ToList());
        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            Items.Add(refreshToken);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider
    {
        private DateTime _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
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
}
