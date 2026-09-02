using DogPlatform.Identity.Application.Features.Authentication.External;
using DogPlatform.Identity.Application.Communication;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Aggregates.ExternalLogin;
using DogPlatform.Identity.Domain.Aggregates.RefreshToken;
using DogPlatform.Identity.Domain.Aggregates.User;
using DogPlatform.Identity.Domain.Legal;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.Logging;
using DogPlatform.Identity.Infrastructure.Authentication;
using DogPlatform.Identity.Infrastructure.Authentication.External;
using DogPlatform.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text;

namespace DogPlatform.Identity.Tests;

public sealed class ExternalAuthTests
{
    private static readonly DateTime Now = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    [Fact]
    public async Task ValidGoogleIdentity_CreatesPasswordlessUserAndDogPlatformSession()
    {
        var fixture = Fixture(Identity(ExternalAuthProvider.Google));
        var result = await fixture.Authenticate(ExternalAuthProvider.Google);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsAuthenticated);
        var user = Assert.Single(fixture.Users.Items);
        Assert.False(user.HasPassword);
        Assert.True(user.IsEmailConfirmed);
        Assert.Null(user.ProfilePhotoUrl);
        Assert.Equal(user.Id, result.Value.Session!.UserId);
        Assert.Single(fixture.RefreshTokens.Items);
        Assert.Single(fixture.ExternalLogins.Items);
    }

    [Fact]
    public async Task ExistingGoogleLogin_ReturnsSameUserWithoutCreatingAnother()
    {
        var identity = Identity(ExternalAuthProvider.Google);
        var user = ExternalUser(identity.Email!);
        var fixture = Fixture(identity, user,
            ExternalLogin.Create(user.Id, identity.Provider, identity.ProviderUserId, identity.Email, Now));

        var first = await fixture.Authenticate(identity.Provider);
        var second = await fixture.Authenticate(identity.Provider);

        Assert.Equal(user.Id, first.Value.Session!.UserId);
        Assert.Equal(user.Id, second.Value.Session!.UserId);
        Assert.Single(fixture.Users.Items);
        Assert.Single(fixture.ExternalLogins.Items);
    }

    [Theory]
    [InlineData(ExternalValidationFailure.InvalidToken, "EXTERNAL_TOKEN_INVALID")]
    [InlineData(ExternalValidationFailure.ExpiredToken, "EXTERNAL_TOKEN_EXPIRED")]
    [InlineData(ExternalValidationFailure.ProviderUnavailable, "EXTERNAL_LOGIN_FAILED")]
    [InlineData(ExternalValidationFailure.ProviderNotConfigured, "EXTERNAL_PROVIDER_NOT_CONFIGURED")]
    public async Task ProviderValidationFailures_AreMappedToSafeErrors(
        ExternalValidationFailure failure, string expectedCode)
    {
        var fixture = Fixture(null, failure: failure);
        var result = await fixture.Authenticate(ExternalAuthProvider.Google);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task IncorrectAudience_IsRejectedAsInvalidToken()
    {
        var fixture = Fixture(null, failure: ExternalValidationFailure.InvalidToken);
        var result = await fixture.Authenticate(ExternalAuthProvider.Google);
        Assert.Equal("EXTERNAL_TOKEN_INVALID", result.Error.Code);
    }

    [Fact]
    public async Task ExistingEmail_IsNeverAutomaticallyLinked()
    {
        var identity = Identity(ExternalAuthProvider.Google);
        var fixture = Fixture(identity, ExternalUser(identity.Email!));
        var result = await fixture.Authenticate(identity.Provider);

        Assert.Equal("EXTERNAL_ACCOUNT_LINK_REQUIRED", result.Error.Code);
        Assert.Empty(fixture.ExternalLogins.Items);
    }

    [Theory]
    [InlineData(ExternalAuthProvider.Facebook)]
    [InlineData(ExternalAuthProvider.Apple)]
    public async Task MissingEmail_ReturnsRegistrationTicketAndNeverInventsEmail(
        ExternalAuthProvider provider)
    {
        var fixture = Fixture(Identity(provider) with { Email = null, EmailVerified = false });
        var result = await fixture.Authenticate(provider,
            provider == ExternalAuthProvider.Apple ? "nonce" : null);

        Assert.True(result.IsSuccess);
        Assert.Equal("EXTERNAL_EMAIL_REQUIRED", result.Value.ActionCode);
        Assert.Equal("signed-ticket", result.Value.RegistrationToken);
        Assert.Contains("email", result.Value.MissingFields);
        Assert.Empty(fixture.Users.Items);
    }

    [Fact]
    public async Task AppleRelayEmail_IsAcceptedAsARealVerifiedEmail()
    {
        var identity = Identity(ExternalAuthProvider.Apple) with
        {
            Email = "relay@privaterelay.appleid.com"
        };
        var fixture = Fixture(identity);
        var result = await fixture.Authenticate(identity.Provider, nonce: "nonce");

        Assert.True(result.Value.IsAuthenticated);
        Assert.Equal(identity.Email, Assert.Single(fixture.Users.Items).Email.Value);
    }

    [Fact]
    public async Task AppleSubsequentLogin_DoesNotNeedNameOrEmailAgain()
    {
        var initial = Identity(ExternalAuthProvider.Apple);
        var user = ExternalUser(initial.Email!);
        var login = ExternalLogin.Create(user.Id, initial.Provider,
            initial.ProviderUserId, initial.Email, Now);
        var fixture = Fixture(initial with
        {
            Email = null, FirstName = null, LastName = null, EmailVerified = false
        }, user, login);

        var result = await fixture.Authenticate(initial.Provider, nonce: "nonce");
        Assert.Equal(user.Id, result.Value.Session!.UserId);
    }

    [Fact]
    public async Task AuthenticatedLink_DoesNotAllowTakingExternalIdentityLinkedToAnotherUser()
    {
        var identity = Identity(ExternalAuthProvider.Google);
        var owner = ExternalUser("owner@example.com");
        var attacker = ExternalUser("attacker@example.com");
        var fixture = Fixture(identity, attacker,
            ExternalLogin.Create(owner.Id, identity.Provider, identity.ProviderUserId, identity.Email, Now));
        fixture.Users.Items.Add(owner);
        var handler = new LinkExternalLoginCommandHandler(fixture.Validator,
            fixture.ExternalLogins, fixture.Users, fixture.UnitOfWork, new FixedTime());

        var result = await handler.Handle(new LinkExternalLoginCommand(
            attacker.Id, identity.Provider, "credential"), default);
        Assert.Equal("EXTERNAL_LOGIN_ALREADY_LINKED", result.Error.Code);
    }

    [Fact]
    public async Task FacebookEmail_IsNotAssumedVerified()
    {
        var fixture = Fixture(Identity(ExternalAuthProvider.Facebook) with { EmailVerified = false });
        var result = await fixture.Authenticate(ExternalAuthProvider.Facebook);
        Assert.Equal("EXTERNAL_REGISTRATION_REQUIRED", result.Value.ActionCode);
        Assert.Contains("emailVerification", result.Value.MissingFields);
    }

    [Fact]
    public async Task ExternalJwtUsesTheSameUserObjectAsPasswordLoginPipeline()
    {
        var user = ExternalUser("claims@example.com");
        var jwt = new RecordingJwtGenerator();
        _ = await ExternalAuthSupport.IssueSessionAsync(user, Now, jwt,
            new FakeRefreshTokenGenerator(), new FakeRefreshTokenRepository(), default);
        Assert.Same(user, jwt.LastUser);
    }

    [Fact]
    public void ExternalLoginUniqueIdentity_IsRepresentedByProviderAndProviderUserId()
    {
        var login = ExternalLogin.Create(Guid.NewGuid(), ExternalAuthProvider.Google,
            "stable-sub", "person@example.com", Now);
        Assert.Equal(ExternalAuthProvider.Google, login.Provider);
        Assert.Equal("stable-sub", login.ProviderUserId);
        Assert.DoesNotContain("token", typeof(ExternalLogin).GetProperties()
            .Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("idToken")]
    [InlineData("accessToken")]
    [InlineData("credential")]
    [InlineData("registrationToken")]
    public void ExternalSecrets_AreRedactedFromRequestLogs(string propertyName)
    {
        const string secret = "provider-secret-token-value";
        var sanitized = new RequestSanitizer().SanitizeJson(
            $"{{\"{propertyName}\":\"{secret}\",\"provider\":\"Google\"}}");
        Assert.DoesNotContain(secret, sanitized);
        Assert.Contains("***", sanitized);
    }

    [Fact]
    public void EfModel_UsesUniqueProviderIdentityAndNullablePasswordColumns()
    {
        using var context = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer("Server=(local);Database=unused;Trusted_Connection=True")
            .Options);
        var external = context.Model.FindEntityType(typeof(ExternalLogin))!;
        var unique = Assert.Single(external.GetIndexes(), index => index.IsUnique);
        Assert.Equal([nameof(ExternalLogin.Provider), nameof(ExternalLogin.ProviderUserId)],
            unique.Properties.Select(x => x.Name));
        var user = context.Model.FindEntityType(typeof(User))!;
        Assert.True(user.FindProperty(nameof(User.PasswordHash))!.IsNullable);
        Assert.True(user.FindProperty(nameof(User.PasswordSalt))!.IsNullable);
    }

    [Fact]
    public void RegistrationTicket_IsSignedExpiresAndDoesNotTrustTampering()
    {
        var service = new ExternalRegistrationTicketService(Options.Create(
            new ExternalRegistrationOptions
            {
                TicketSecret = "a-strong-test-secret-with-more-than-32-bytes",
                TicketLifetimeMinutes = 10
            }));
        var ticket = service.Issue(Identity(ExternalAuthProvider.Apple), Now);
        var valid = service.Validate(ticket, Now.AddMinutes(1));
        var tampered = service.Validate(ticket[..^1] + (ticket[^1] == 'a' ? 'b' : 'a'), Now);
        var expired = service.Validate(ticket, Now.AddMinutes(11));

        Assert.True(valid.IsSuccess);
        Assert.Equal("stable-provider-user-id", valid.Identity!.ProviderUserId);
        Assert.False(tampered.IsSuccess);
        Assert.False(expired.IsSuccess);
    }

    [Fact]
    public void ExternalJwt_ContainsTheStandardDogPlatformClaims()
    {
        var generator = new JwtTokenGenerator(Options.Create(new JwtOptions
        {
            Issuer = "DogPlatform.Identity",
            Audience = "DogPlatform",
            Secret = "a-strong-test-secret-with-more-than-32-bytes",
            AccessTokenMinutes = 15
        }), new FixedTime());
        var user = ExternalUser("claims@example.com");
        var token = new JwtSecurityTokenHandler().ReadJwtToken(
            generator.GenerateAccessToken(user).AccessToken);

        Assert.Equal(user.Id.ToString(), token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email.Value, token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.FullName.FirstName, token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.GivenName).Value);
        Assert.Equal(user.FullName.LastName, token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.FamilyName).Value);
    }

    [Fact]
    public void GoogleJwtValidation_VerifiesSignatureIssuerAudienceAndExpiration()
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test-key" };
        var valid = ProviderToken(key, "google-client", DateTime.UtcNow.AddMinutes(5));
        var wrongAudience = ProviderToken(key, "attacker-client", DateTime.UtcNow.AddMinutes(5));
        var expired = ProviderToken(key, "google-client", DateTime.UtcNow.AddMinutes(-5));

        var principal = OidcExternalIdentityValidator.ValidateJwt(valid, [key],
            ["https://accounts.google.com", "accounts.google.com"], ["google-client"]);
        Assert.Equal("provider-sub", principal.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        Assert.Throws<SecurityTokenInvalidAudienceException>(() =>
            OidcExternalIdentityValidator.ValidateJwt(wrongAudience, [key],
                ["https://accounts.google.com"], ["google-client"]));
        Assert.Throws<SecurityTokenExpiredException>(() =>
            OidcExternalIdentityValidator.ValidateJwt(expired, [key],
                ["https://accounts.google.com"], ["google-client"]));
    }

    [Fact]
    public async Task CompleteRegistration_WithUserSuppliedEmail_RequiresVerificationAndCreatesNoPassword()
    {
        var identity = Identity(ExternalAuthProvider.Facebook) with
        {
            Email = null,
            EmailVerified = false
        };
        var users = new FakeUserRepository();
        var logins = new FakeExternalLoginRepository();
        var emailSender = new FakeEmailSender();
        var handler = new CompleteExternalRegistrationCommandHandler(
            new ValidTickets(identity), logins, users, new FakeRefreshTokenRepository(),
            new EmptyLegalDocuments(), new FakeLegalConsents(), new FakeVerificationCodes(),
            emailSender, new RecordingJwtGenerator(), new FakeRefreshTokenGenerator(),
            new FakeUnitOfWork(), new FixedTime());

        var result = await handler.Handle(new CompleteExternalRegistrationCommand(
            "signed-ticket", "person@example.com", null, null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("EXTERNAL_EMAIL_VERIFICATION_REQUIRED", result.Value.ActionCode);
        var user = Assert.Single(users.Items);
        Assert.False(user.HasPassword);
        Assert.False(user.IsEmailConfirmed);
        Assert.Single(logins.Items);
        Assert.Equal("person@example.com", emailSender.Email);
    }

    [Fact]
    public async Task FacebookAccessToken_IsDebuggedAndSnakeCaseProfileIsMapped()
    {
        var messages = new Queue<HttpResponseMessage>([
            Json("{\"data\":{\"is_valid\":true,\"app_id\":\"app-id\",\"user_id\":\"fb-123\",\"expires_at\":4102444800}}"),
            Json("{\"id\":\"fb-123\",\"email\":\"fb@example.com\",\"first_name\":\"Face\",\"last_name\":\"Book\",\"picture\":{\"data\":{\"url\":\"https://example.com/p.jpg\"}}}")
        ]);
        var capture = new CapturingHttpHandler(messages);
        var validator = new FacebookIdentityValidator(new HttpClient(capture)
        {
            BaseAddress = new Uri("https://graph.facebook.com/")
        }, Options.Create(new FacebookExternalAuthOptions
        {
            AppId = "app-id", AppSecret = "app-secret", GraphApiVersion = "v23.0"
        }), NullLogger<FacebookIdentityValidator>.Instance);

        var result = await validator.ValidateAsync("user-access-token", null, default);

        Assert.True(result.IsSuccess);
        Assert.Equal("fb-123", result.Identity!.ProviderUserId);
        Assert.Equal("Face", result.Identity.FirstName);
        Assert.False(result.Identity.EmailVerified);
        Assert.All(capture.Uris, uri =>
        {
            Assert.DoesNotContain("user-access-token", uri);
            Assert.DoesNotContain("app-secret", uri);
        });
    }

    private static ExternalIdentity Identity(ExternalAuthProvider provider) => new(
        provider, "stable-provider-user-id", "new@example.com", true,
        "New", "User", "https://provider.example/photo.jpg");

    private static User ExternalUser(string email) => User.RegisterExternal(
        Guid.NewGuid(), FullName.Create("Existing", "User").Value,
        Email.Create(email).Value, true, Now);

    private static string ProviderToken(SecurityKey key, string audience, DateTime expires)
    {
        var token = new JwtSecurityToken("https://accounts.google.com", audience,
            [new Claim(JwtRegisteredClaimNames.Sub, "provider-sub")],
            notBefore: DateTime.UtcNow.AddMinutes(-10), expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        token.Header[JwtHeaderParameterNames.Kid] = key.KeyId;
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private static FixtureState Fixture(ExternalIdentity? identity, User? user = null,
        ExternalLogin? login = null, ExternalValidationFailure failure = ExternalValidationFailure.None)
    {
        var users = new FakeUserRepository();
        if (user is not null) users.Items.Add(user);
        var logins = new FakeExternalLoginRepository();
        if (login is not null) logins.Items.Add(login);
        var validator = new FakeValidator(identity, failure);
        var refresh = new FakeRefreshTokenRepository();
        var unit = new FakeUnitOfWork();
        var handler = new ExternalAuthCommandHandler(validator, new FakeTickets(), logins,
            users, refresh, new EmptyLegalDocuments(), new FakeLegalConsents(),
            new RecordingJwtGenerator(), new FakeRefreshTokenGenerator(), unit, new FixedTime());
        return new FixtureState(handler, validator, users, logins, refresh, unit);
    }

    private sealed record FixtureState(ExternalAuthCommandHandler Handler, FakeValidator Validator,
        FakeUserRepository Users, FakeExternalLoginRepository ExternalLogins,
        FakeRefreshTokenRepository RefreshTokens, FakeUnitOfWork UnitOfWork)
    {
        public Task<DogPlatform.SharedKernel.Primitives.Result<ExternalAuthOutcome>> Authenticate(
            ExternalAuthProvider provider, string? nonce = null) => Handler.Handle(
                new ExternalAuthCommand(provider, "credential", nonce), default);
    }

    private sealed class FakeValidator(ExternalIdentity? identity, ExternalValidationFailure failure)
        : IExternalIdentityValidator
    {
        public Task<ExternalIdentityValidationResult> ValidateAsync(ExternalAuthProvider provider,
            string credential, string? nonce = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(identity is null
                ? ExternalIdentityValidationResult.Failed(failure)
                : ExternalIdentityValidationResult.Success(identity));
    }

    private sealed class FakeTickets : IExternalRegistrationTicketService
    {
        public string Issue(ExternalIdentity identity, DateTime utcNow) => "signed-ticket";
        public ExternalIdentityValidationResult Validate(string ticket, DateTime utcNow) =>
            ExternalIdentityValidationResult.Failed(ExternalValidationFailure.InvalidToken);
    }

    private sealed class ValidTickets(ExternalIdentity identity) : IExternalRegistrationTicketService
    {
        public string Issue(ExternalIdentity value, DateTime utcNow) => "signed-ticket";
        public ExternalIdentityValidationResult Validate(string ticket, DateTime utcNow) =>
            ExternalIdentityValidationResult.Success(identity);
    }

    private sealed class FakeVerificationCodes : IEmailVerificationCodeService
    {
        public EmailVerificationCodeResult Generate() => new("123456", "hashed-code");
        public bool Verify(string code, string expectedHash) => code == "123456";
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public string? Email { get; private set; }
        public Task SendVerificationCodeAsync(string email, string code,
            CancellationToken cancellationToken)
        { Email = email; return Task.CompletedTask; }
        public Task SendPasswordResetCodeAsync(string email, string code, int expirationMinutes,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeExternalLoginRepository : IExternalLoginRepository
    {
        public List<ExternalLogin> Items { get; } = [];
        public Task<ExternalLogin?> GetAsync(ExternalAuthProvider provider, string providerUserId,
            CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(
                x => x.Provider == provider && x.ProviderUserId == providerUserId));
        public Task AddAsync(ExternalLogin externalLogin, CancellationToken cancellationToken = default)
        { Items.Add(externalLogin); return Task.CompletedTask; }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Items { get; } = [];
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Email.Value == email.Value));
        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        { Items.Add(user); return Task.CompletedTask; }
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(x => x.Email.Value == email.Value));
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> Items { get; } = [];
        public Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Token == token));
        public Task<IReadOnlyCollection<RefreshToken>> GetActiveByUserIdAsync(Guid userId, DateTime utcNow,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<RefreshToken>>(
                Items.Where(x => x.UserId == userId && x.IsActive(utcNow)).ToList());
        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        { Items.Add(refreshToken); return Task.CompletedTask; }
        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmptyLegalDocuments : ILegalDocumentRepository
    {
        public Task<IReadOnlyList<LegalDocument>> GetActiveRequiredAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LegalDocument>>([]);
        public Task<LegalDocument?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<LegalDocument?>(null);
        public Task<IReadOnlyList<LegalDocument>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LegalDocument>>([]);
    }

    private sealed class FakeLegalConsents : IUserLegalConsentRepository
    {
        public Task<IReadOnlyList<UserLegalConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserLegalConsent>>([]);
        public Task<bool> ExistsAsync(Guid userId, Guid legalDocumentId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(UserLegalConsent consent, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRangeAsync(IEnumerable<UserLegalConsent> consents, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingJwtGenerator : IJwtTokenGenerator
    {
        public User? LastUser { get; private set; }
        public JwtTokenResult GenerateAccessToken(User user)
        { LastUser = user; return new("dogplatform-jwt", Now.AddMinutes(15)); }
    }

    private sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public RefreshTokenResult Generate(DateTime utcNow) => new(Guid.NewGuid().ToString("N"), utcNow.AddDays(30));
    }

    private sealed class FakeUnitOfWork : Application.IIdentityUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    private sealed class CapturingHttpHandler(Queue<HttpResponseMessage> responses) : HttpMessageHandler
    {
        public List<string> Uris { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Uris.Add(request.RequestUri!.ToString());
            return Task.FromResult(responses.Dequeue());
        }
    }
}
