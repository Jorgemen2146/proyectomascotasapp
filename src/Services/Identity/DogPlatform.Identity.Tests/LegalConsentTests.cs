using DogPlatform.Identity.Application;
using DogPlatform.Identity.Application.Communication;
using DogPlatform.Identity.Application.Features.Authentication.Register;
using DogPlatform.Identity.Application.Features.Legal;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Aggregates.User;
using DogPlatform.Identity.Domain.Legal;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;

namespace DogPlatform.Identity.Tests;

public sealed class LegalConsentTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetActiveDocuments_ReturnsOnlyActiveRequiredDocuments()
    {
        var repository = new FakeLegalDocumentRepository(Terms(), Privacy(), Privacy("2.0", false));
        var result = await new GetActiveLegalDocumentsQueryHandler(repository)
            .Handle(new GetActiveLegalDocumentsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, document => Assert.True(document.RequiresAcceptance));
    }

    [Fact]
    public async Task Register_WithoutTerms_ReturnsConsentRequired()
    {
        var fixture = new RegisterFixture();
        var result = await fixture.Handle([Consent("PrivacyPolicy", "1.0")]);
        Assert.Equal("LEGAL_CONSENT_REQUIRED", result.Error.Code);
        Assert.Empty(fixture.Users.Added);
    }

    [Fact]
    public async Task Register_WithoutPrivacy_ReturnsConsentRequired()
    {
        var fixture = new RegisterFixture();
        var result = await fixture.Handle([Consent("TermsAndConditions", "1.0")]);
        Assert.Equal("LEGAL_CONSENT_REQUIRED", result.Error.Code);
        Assert.Empty(fixture.Users.Added);
    }

    [Fact]
    public async Task Register_WithWrongVersion_ReturnsVersionInvalid()
    {
        var fixture = new RegisterFixture();
        var result = await fixture.Handle([
            Consent("TermsAndConditions", "0.9"), Consent("PrivacyPolicy", "1.0")]);
        Assert.Equal("LEGAL_DOCUMENT_VERSION_INVALID", result.Error.Code);
        Assert.Empty(fixture.Users.Added);
    }

    [Fact]
    public async Task Register_WithRequiredVersions_SavesUserAndTwoConsentsTogether()
    {
        var fixture = new RegisterFixture();
        var result = await fixture.Handle([Consent("TermsAndConditions", "1.0"),
            Consent("PrivacyPolicy", "1.0")]);

        Assert.True(result.IsSuccess);
        Assert.Single(fixture.Users.Added);
        Assert.Equal(2, fixture.Consents.Items.Count);
        Assert.All(fixture.Consents.Items, consent => Assert.Equal(result.Value.UserId, consent.UserId));
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task AcceptDocument_WhenAlreadyAccepted_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var terms = Terms();
        var consents = new FakeConsentRepository(
            UserLegalConsent.Accept(Guid.NewGuid(), userId, terms.Id, UtcNow));
        var handler = new AcceptLegalConsentCommandHandler(
            new FakeLegalDocumentRepository(terms), consents, new FakeUnitOfWork(),
            new TestTimeProvider(UtcNow));

        var result = await handler.Handle(
            new AcceptLegalConsentCommand(userId, terms.Id), CancellationToken.None);

        Assert.Equal("LEGAL_CONSENT_ALREADY_EXISTS", result.Error.Code);
        Assert.Single(consents.Items);
    }

    [Fact]
    public async Task LegalStatus_WhenAllRequiredAccepted_IsUpToDate()
    {
        var userId = Guid.NewGuid();
        var terms = Terms();
        var privacy = Privacy();
        var consents = new FakeConsentRepository(
            UserLegalConsent.Accept(Guid.NewGuid(), userId, terms.Id, UtcNow),
            UserLegalConsent.Accept(Guid.NewGuid(), userId, privacy.Id, UtcNow));

        var status = await new GetLegalStatusQueryHandler(
            new FakeLegalDocumentRepository(terms, privacy), consents)
            .Handle(new GetLegalStatusQuery(userId), CancellationToken.None);

        Assert.True(status.IsUpToDate);
        Assert.Empty(status.PendingDocuments);
    }

    [Fact]
    public async Task LegalStatus_WithNewActiveVersion_ReturnsItAsPending()
    {
        var userId = Guid.NewGuid();
        var oldPrivacy = Privacy();
        var newPrivacy = Privacy("2.0");
        var consents = new FakeConsentRepository(
            UserLegalConsent.Accept(Guid.NewGuid(), userId, oldPrivacy.Id, UtcNow));

        var status = await new GetLegalStatusQueryHandler(
            new FakeLegalDocumentRepository(newPrivacy), consents)
            .Handle(new GetLegalStatusQuery(userId), CancellationToken.None);

        Assert.False(status.IsUpToDate);
        Assert.Equal("2.0", Assert.Single(status.PendingDocuments).Version);
    }

    [Fact]
    public async Task AcceptNewVersion_SavesASeparateConsent()
    {
        var userId = Guid.NewGuid();
        var oldPrivacy = Privacy();
        var newPrivacy = Privacy("2.0");
        var documents = new FakeLegalDocumentRepository(oldPrivacy, newPrivacy);
        var consents = new FakeConsentRepository(
            UserLegalConsent.Accept(Guid.NewGuid(), userId, oldPrivacy.Id, UtcNow.AddDays(-10)));
        var unitOfWork = new FakeUnitOfWork();

        var accepted = await new AcceptLegalConsentCommandHandler(
            documents, consents, unitOfWork, new TestTimeProvider(UtcNow))
            .Handle(new AcceptLegalConsentCommand(userId, newPrivacy.Id), CancellationToken.None);

        Assert.True(accepted.IsSuccess);
        Assert.Equal(2, consents.Items.Count);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ConsentHistory_ReturnsBothAcceptedVersions()
    {
        var userId = Guid.NewGuid();
        var oldPrivacy = Privacy();
        var newPrivacy = Privacy("2.0");
        var documents = new FakeLegalDocumentRepository(oldPrivacy, newPrivacy);
        var consents = new FakeConsentRepository(
            UserLegalConsent.Accept(Guid.NewGuid(), userId, oldPrivacy.Id, UtcNow.AddDays(-10)),
            UserLegalConsent.Accept(Guid.NewGuid(), userId, newPrivacy.Id, UtcNow));

        var history = await new GetLegalConsentHistoryQueryHandler(documents, consents)
            .Handle(new GetLegalConsentHistoryQuery(userId), CancellationToken.None);

        Assert.Equal(2, history.Count);
        Assert.Contains(history, item => item.Version == "1.0");
        Assert.Contains(history, item => item.Version == "2.0");
    }

    [Fact]
    public async Task Register_DoesNotRequireInactiveDocument()
    {
        var fixture = new RegisterFixture(Privacy("2.0", false));
        var result = await fixture.Handle([Consent("TermsAndConditions", "1.0"),
            Consent("PrivacyPolicy", "1.0")]);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, fixture.Consents.Items.Count);
    }

    private static LegalConsentSelection Consent(string type, string version) => new(type, version);

    private static LegalDocument Terms(string version = "1.0", bool active = true) =>
        LegalDocument.Create(Guid.NewGuid(), LegalDocumentType.TermsAndConditions, version,
            "Terms", "Test content", UtcNow, UtcNow, active, true, UtcNow);

    private static LegalDocument Privacy(string version = "1.0", bool active = true) =>
        LegalDocument.Create(Guid.NewGuid(), LegalDocumentType.PrivacyPolicy, version,
            "Privacy", "Test content", UtcNow, UtcNow, active, true, UtcNow);

    private sealed class RegisterFixture
    {
        public FakeUserRepository Users { get; } = new();
        public FakeConsentRepository Consents { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        private readonly RegisterUserCommandHandler _handler;

        public RegisterFixture(params LegalDocument[] extraDocuments)
        {
            var documents = new FakeLegalDocumentRepository([Terms(), Privacy(), .. extraDocuments]);
            _handler = new RegisterUserCommandHandler(Users, UnitOfWork,
                new FakePasswordHasher(), new FakeVerificationCodeService(),
                new FakeEmailSender(), new TestTimeProvider(UtcNow), documents, Consents);
        }

        public Task<DogPlatform.SharedKernel.Primitives.Result<RegisterUserResponse>> Handle(
            IReadOnlyList<LegalConsentSelection> consents) => _handler.Handle(
                new RegisterUserCommand("Test", "User", "new@example.com", "Password123",
                    null, consents), CancellationToken.None);
    }

    private sealed class FakeLegalDocumentRepository(params LegalDocument[] documents)
        : ILegalDocumentRepository
    {
        public Task<IReadOnlyList<LegalDocument>> GetActiveRequiredAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LegalDocument>>(documents
                .Where(document => document.IsActive && document.RequiresAcceptance).ToList());

        public Task<LegalDocument?> GetActiveByIdAsync(Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(documents.FirstOrDefault(document => document.Id == id && document.IsActive));

        public Task<IReadOnlyList<LegalDocument>> GetByIdsAsync(IEnumerable<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            var set = ids.ToHashSet();
            return Task.FromResult<IReadOnlyList<LegalDocument>>(
                documents.Where(document => set.Contains(document.Id)).ToList());
        }
    }

    private sealed class FakeConsentRepository(params UserLegalConsent[] initial)
        : IUserLegalConsentRepository
    {
        public List<UserLegalConsent> Items { get; } = [.. initial];
        public Task<IReadOnlyList<UserLegalConsent>> GetByUserIdAsync(Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserLegalConsent>>(
                Items.Where(consent => consent.UserId == userId).ToList());
        public Task<bool> ExistsAsync(Guid userId, Guid legalDocumentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(consent => consent.UserId == userId
                && consent.LegalDocumentId == legalDocumentId));
        public Task AddAsync(UserLegalConsent consent,
            CancellationToken cancellationToken = default) { Items.Add(consent); return Task.CompletedTask; }
        public Task AddRangeAsync(IEnumerable<UserLegalConsent> consents,
            CancellationToken cancellationToken = default) { Items.AddRange(consents); return Task.CompletedTask; }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Added { get; } = [];
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task AddAsync(User user, CancellationToken cancellationToken = default) { Added.Add(user); return Task.CompletedTask; }
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class FakeUnitOfWork : IIdentityUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        { SaveCount++; return Task.FromResult(1); }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordHashResult Hash(string password) => new("hash", "salt");
        public bool Verify(string password, string hash, string salt) => true;
    }

    private sealed class FakeVerificationCodeService : IEmailVerificationCodeService
    {
        public EmailVerificationCodeResult Generate() => new("123456", "hash:123456");
        public bool Verify(string code, string expectedHash) => true;
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendVerificationCodeAsync(string email, string code,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SendPasswordResetCodeAsync(string email, string code,
            int expirationMinutes, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
