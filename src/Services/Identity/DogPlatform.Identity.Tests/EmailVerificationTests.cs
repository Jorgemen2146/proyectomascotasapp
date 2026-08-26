using DogPlatform.Identity.Application;
using DogPlatform.Identity.Application.Communication;
using DogPlatform.Identity.Application.Features.Authentication.Login;
using DogPlatform.Identity.Application.Features.Authentication.ResendVerification;
using DogPlatform.Identity.Application.Features.Authentication.VerifyEmail;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Aggregates.RefreshToken;
using DogPlatform.Identity.Domain.Aggregates.User;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;

namespace DogPlatform.Identity.Tests;

public sealed class EmailVerificationTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task VerifyEmail_WithValidCode_ConfirmsEmailAndInvalidatesCode()
    {
        var user = CreateUser();
        var codes = new FakeVerificationCodeService();
        IssueCode(user, codes, UtcNow);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new VerifyEmailCommandHandler(
            new FakeUserRepository(user),
            unitOfWork,
            codes,
            new TestTimeProvider(UtcNow),
            new VerifyEmailValidator());

        var result = await handler.Handle(
            new VerifyEmailCommand(user.Email.Value, codes.Code),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsEmailConfirmed);
        Assert.Equal(UtcNow, user.EmailConfirmedAt);
        Assert.Null(user.EmailVerificationCodeHash);
        Assert.Null(user.EmailVerificationCodeExpiresAt);
        Assert.Equal(0, user.EmailVerificationAttempts);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task VerifyEmail_WithIncorrectCode_RecordsFailedAttempt()
    {
        var user = CreateUser();
        var codes = new FakeVerificationCodeService();
        IssueCode(user, codes, UtcNow);
        var handler = CreateVerifyHandler(user, codes, UtcNow);

        var result = await handler.Handle(
            new VerifyEmailCommand(user.Email.Value, "000000"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("EmailVerification.InvalidCode", result.Error.Code);
        Assert.Equal(1, user.EmailVerificationAttempts);
        Assert.NotNull(user.EmailVerificationCodeHash);
        Assert.False(user.IsEmailConfirmed);
    }

    [Fact]
    public async Task VerifyEmail_WithExpiredCode_InvalidatesCode()
    {
        var user = CreateUser();
        var codes = new FakeVerificationCodeService();
        var generated = codes.Generate();
        user.IssueEmailVerificationCode(generated.Hash, UtcNow.AddMinutes(-1), UtcNow.AddMinutes(-11));
        var handler = CreateVerifyHandler(user, codes, UtcNow);

        var result = await handler.Handle(
            new VerifyEmailCommand(user.Email.Value, codes.Code),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("EmailVerification.CodeExpired", result.Error.Code);
        Assert.Null(user.EmailVerificationCodeHash);
        Assert.Null(user.EmailVerificationCodeExpiresAt);
    }

    [Fact]
    public async Task VerifyEmail_WhenAlreadyConfirmed_ReturnsConflict()
    {
        var user = CreateUser();
        user.ConfirmEmail(UtcNow);
        var codes = new FakeVerificationCodeService();
        var handler = CreateVerifyHandler(user, codes, UtcNow);

        var result = await handler.Handle(
            new VerifyEmailCommand(user.Email.Value, codes.Code),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.EmailAlreadyConfirmed", result.Error.Code);
    }

    [Fact]
    public async Task VerifyEmail_OnFifthIncorrectCode_InvalidatesCodeAndRequiresResend()
    {
        var user = CreateUser();
        var codes = new FakeVerificationCodeService();
        IssueCode(user, codes, UtcNow);
        var handler = CreateVerifyHandler(user, codes, UtcNow);

        for (var attempt = 1; attempt <= User.MaximumEmailVerificationAttempts; attempt++)
        {
            var result = await handler.Handle(
                new VerifyEmailCommand(user.Email.Value, "000000"),
                CancellationToken.None);

            Assert.True(result.IsFailure);
            if (attempt == User.MaximumEmailVerificationAttempts)
                Assert.Equal("EmailVerification.AttemptsExceeded", result.Error.Code);
        }

        Assert.Equal(User.MaximumEmailVerificationAttempts, user.EmailVerificationAttempts);
        Assert.Null(user.EmailVerificationCodeHash);
        Assert.Null(user.EmailVerificationCodeExpiresAt);
    }

    [Fact]
    public async Task ResendVerification_BeforeCooldown_DoesNotSendOrReplaceCode()
    {
        var user = CreateUser();
        var codes = new FakeVerificationCodeService();
        IssueCode(user, codes, UtcNow);
        var originalHash = user.EmailVerificationCodeHash;
        var emailSender = new FakeEmailSender();
        var handler = new ResendVerificationCommandHandler(
            new FakeUserRepository(user),
            new FakeUnitOfWork(),
            codes,
            emailSender,
            new TestTimeProvider(UtcNow.AddSeconds(30)),
            new ResendVerificationValidator());

        var result = await handler.Handle(
            new ResendVerificationCommand(user.Email.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(emailSender.Messages);
        Assert.Equal(originalHash, user.EmailVerificationCodeHash);
    }

    [Fact]
    public async Task ResendVerification_AfterCooldown_ReplacesAndSendsCode()
    {
        var user = CreateUser();
        var codes = new FakeVerificationCodeService();
        var oldCode = new EmailVerificationCodeResult("111111", "hash:111111");
        user.IssueEmailVerificationCode(oldCode.Hash, UtcNow, UtcNow.AddMinutes(-2));
        var emailSender = new FakeEmailSender();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ResendVerificationCommandHandler(
            new FakeUserRepository(user),
            unitOfWork,
            codes,
            emailSender,
            new TestTimeProvider(UtcNow),
            new ResendVerificationValidator());

        var result = await handler.Handle(
            new ResendVerificationCommand(user.Email.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(emailSender.Messages);
        Assert.Equal((user.Email.Value, codes.Code), emailSender.Messages[0]);
        Assert.Equal($"hash:{codes.Code}", user.EmailVerificationCodeHash);
        Assert.Equal(UtcNow.AddMinutes(10), user.EmailVerificationCodeExpiresAt);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Login_WithUnverifiedEmail_DoesNotIssueTokens()
    {
        var user = CreateUser();
        var jwt = new FakeJwtTokenGenerator();
        var refresh = new FakeRefreshTokenGenerator();
        var handler = CreateLoginHandler(user, jwt, refresh);

        var result = await handler.Handle(
            new LoginCommand(user.Email.Value, "Password123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("EMAIL_NOT_VERIFIED", result.Error.Code);
        Assert.Equal(0, jwt.GenerateCount);
        Assert.Equal(0, refresh.GenerateCount);
    }

    [Fact]
    public async Task Login_WithVerifiedEmail_IssuesTokens()
    {
        var user = CreateUser();
        user.ConfirmEmail(UtcNow.AddMinutes(-1));
        var jwt = new FakeJwtTokenGenerator();
        var refresh = new FakeRefreshTokenGenerator();
        var handler = CreateLoginHandler(user, jwt, refresh);

        var result = await handler.Handle(
            new LoginCommand(user.Email.Value, "Password123"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, jwt.GenerateCount);
        Assert.Equal(1, refresh.GenerateCount);
    }

    private static VerifyEmailCommandHandler CreateVerifyHandler(
        User user,
        IEmailVerificationCodeService codes,
        DateTime utcNow) =>
        new(
            new FakeUserRepository(user),
            new FakeUnitOfWork(),
            codes,
            new TestTimeProvider(utcNow),
            new VerifyEmailValidator());

    private static LoginCommandHandler CreateLoginHandler(
        User user,
        FakeJwtTokenGenerator jwt,
        FakeRefreshTokenGenerator refresh) =>
        new(
            new FakeUserRepository(user),
            new FakeRefreshTokenRepository(),
            new FakeUnitOfWork(),
            new FakePasswordHasher(),
            jwt,
            refresh,
            new TestTimeProvider(UtcNow));

    private static User CreateUser()
    {
        var fullName = FullName.Create("Test", "User").Value;
        var email = Email.Create("user@example.com").Value;
        return User.Register(Guid.NewGuid(), fullName, email, "hash", "salt", UtcNow.AddHours(-1));
    }

    private static void IssueCode(User user, FakeVerificationCodeService codes, DateTime sentAt)
    {
        var generated = codes.Generate();
        user.IssueEmailVerificationCode(generated.Hash, sentAt.AddMinutes(10), sentAt);
    }

    private sealed class TestTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class FakeVerificationCodeService : IEmailVerificationCodeService
    {
        public string Code { get; } = "483921";

        public EmailVerificationCodeResult Generate() => new(Code, $"hash:{Code}");

        public bool Verify(string code, string expectedHash) => expectedHash == $"hash:{code}";
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<(string Email, string Code)> Messages { get; } = [];

        public Task SendVerificationCodeAsync(
            string email,
            string code,
            CancellationToken cancellationToken)
        {
            Messages.Add((email, code));
            return Task.CompletedTask;
        }

        public Task SendPasswordResetCodeAsync(string email, string code,
            int expirationMinutes, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(user?.Id == id ? user : null);

        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
            Task.FromResult(user?.Email == email ? user : null);

        public Task AddAsync(User newUser, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(User updatedUser, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default) =>
            Task.FromResult(user?.Email == email);
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

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordHashResult Hash(string password) => new("hash", "salt");

        public bool Verify(string password, string hash, string salt) => password == "Password123";
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public int GenerateCount { get; private set; }

        public JwtTokenResult GenerateAccessToken(User user)
        {
            GenerateCount++;
            return new JwtTokenResult("access-token", UtcNow.AddMinutes(15));
        }
    }

    private sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public int GenerateCount { get; private set; }

        public RefreshTokenResult Generate(DateTime utcNow)
        {
            GenerateCount++;
            return new RefreshTokenResult("refresh-token", utcNow.AddDays(7));
        }
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> Added { get; } = [];

        public Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RefreshToken?>(null);

        public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
            Task.FromResult<RefreshToken?>(null);

        public Task<IReadOnlyCollection<RefreshToken>> GetActiveByUserIdAsync(
            Guid userId,
            DateTime utcNow,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<RefreshToken>>(Array.Empty<RefreshToken>());

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            Added.Add(refreshToken);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
