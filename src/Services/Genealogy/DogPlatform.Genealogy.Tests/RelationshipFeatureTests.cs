using DogPlatform.Genealogy.Application.Features.Relationships;
using DogPlatform.Genealogy.Application.Security;
using DogPlatform.Genealogy.Domain.Relationships;
using DogPlatform.Genealogy.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace DogPlatform.Genealogy.Tests;

public sealed class RelationshipFeatureTests
{
    private static readonly Guid Requester = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Target = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Stranger = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Child = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Father = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Mother = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTime Now = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Same_owner_father_is_activated()
    {
        var fixture = Fixture.ForRequester();
        var result = await fixture.AddHandler().Handle(new(Child, Father, "Father"), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(ParentRole.Father, Assert.Single(fixture.Relationships.Items).ParentRole);
    }

    [Fact]
    public async Task Same_owner_mother_is_activated()
    {
        var fixture = Fixture.ForRequester();
        var result = await fixture.AddHandler().Handle(new(Child, Mother, "Mother"), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(ParentRole.Mother, Assert.Single(fixture.Relationships.Items).ParentRole);
    }

    [Fact]
    public async Task Self_relationship_is_rejected()
    {
        var fixture = Fixture.ForRequester();
        var result = await fixture.AddHandler().Handle(new(Child, Child, "Father"), default);
        Assert.Equal("GENEALOGY_SELF_RELATIONSHIP", result.Error.Code);
    }

    [Theory]
    [InlineData("Father")]
    [InlineData("Mother")]
    public async Task Duplicate_parent_role_is_rejected(string roleName)
    {
        var fixture = Fixture.ForRequester();
        var role = Enum.Parse<ParentRole>(roleName);
        var originalParent = role == ParentRole.Father ? Father : Mother;
        fixture.Relationships.Items.Add(PetRelationship.CreateActive(Child, originalParent,
            role, Requester, Now));
        var alternative = Guid.NewGuid();
        fixture.Pets.Add(new(alternative, Requester, "Alternative", 1, null,
            role == ParentRole.Father ? "M" : "F", null, null));

        var result = await fixture.AddHandler().Handle(new(Child, alternative, roleName), default);

        Assert.Equal("GENEALOGY_PARENT_ALREADY_ASSIGNED", result.Error.Code);
    }

    [Fact]
    public async Task Cycle_is_detected()
    {
        var fixture = Fixture.ForRequester();
        fixture.Relationships.Items.Add(PetRelationship.CreateActive(Child, Father,
            ParentRole.Father, Requester, Now));
        var result = await fixture.AddHandler().Handle(new(Father, Child, "Father"), default);
        Assert.Equal("GENEALOGY_CYCLE_DETECTED", result.Error.Code);
    }

    [Fact]
    public async Task Incorrect_parent_sex_is_rejected()
    {
        var fixture = Fixture.ForRequester();
        var result = await fixture.AddHandler().Handle(new(Child, Mother, "Father"), default);
        Assert.Equal("GENEALOGY_PARENT_SEX_MISMATCH", result.Error.Code);
    }

    [Fact]
    public async Task Relationship_is_soft_deleted_by_involved_owner()
    {
        var fixture = Fixture.ForRequester();
        var relationship = PetRelationship.CreateActive(Child, Father, ParentRole.Father, Requester, Now);
        fixture.Relationships.Items.Add(relationship);
        var result = await fixture.DeleteHandler().Handle(new(relationship.Id), default);
        Assert.True(result.IsSuccess);
        Assert.False(relationship.IsActive);
        Assert.NotNull(relationship.DeletedAtUtc);
    }

    [Fact]
    public async Task Uninvolved_user_cannot_delete_relationship()
    {
        var fixture = Fixture.ForRequester();
        var relationship = PetRelationship.CreateActive(Child, Father, ParentRole.Father, Requester, Now);
        fixture.Relationships.Items.Add(relationship);
        fixture.User.UserIdValue = Stranger;
        var result = await fixture.DeleteHandler().Handle(new(relationship.Id), default);
        Assert.Equal("GENEALOGY_FORBIDDEN", result.Error.Code);
    }

    [Fact]
    public async Task Children_are_derived_from_parent_relationships()
    {
        var fixture = Fixture.ForRequester();
        fixture.Relationships.Items.Add(PetRelationship.CreateActive(Child, Father,
            ParentRole.Father, Requester, Now));
        fixture.User.UserIdValue = Requester;
        var result = await fixture.TreeHandler().Handle(new(Father, 1), default);
        Assert.Single(result.Value.Children);
        Assert.Equal(Child, result.Value.Children.Single().Pet.PetId);
    }

    [Fact]
    public async Task Tree_honors_one_and_three_generations()
    {
        var fixture = Fixture.ForRequester();
        var grandparent = Guid.NewGuid();
        fixture.Pets.Add(new(grandparent, Target, "Grandfather", 1, null, "M", null, null));
        fixture.Relationships.Items.Add(PetRelationship.CreateActive(Child, Father,
            ParentRole.Father, Requester, Now));
        fixture.Relationships.Items.Add(PetRelationship.CreateActive(Father, grandparent,
            ParentRole.Father, Target, Now));
        var one = await fixture.TreeHandler().Handle(new(Child, 1), default);
        var three = await fixture.TreeHandler().Handle(new(Child, 3), default);
        Assert.Empty(one.Value.Parents.Single().Parents);
        Assert.Equal(grandparent, three.Value.Parents.Single().Parents.Single().Pet.PetId);
    }

    [Fact]
    public async Task Deleted_and_pending_relationships_do_not_appear_in_tree()
    {
        var fixture = Fixture.ForRequester();
        var relationship = PetRelationship.CreateActive(Child, Father, ParentRole.Father, Requester, Now);
        relationship.SoftDelete(Now.AddMinutes(1));
        fixture.Relationships.Items.Add(relationship);
        fixture.Relationships.Items.Add(PetRelationship.CreatePending(Child, Mother,
            ParentRole.Mother, Requester, Now));
        var result = await fixture.TreeHandler().Handle(new(Child, 3), default);
        Assert.Empty(result.Value.Parents);
    }

    [Fact]
    public async Task External_invitation_stores_hash_not_plaintext()
    {
        var fixture = Fixture.ForRequester();
        var result = await fixture.CreateInvitationHandler().Handle(
            new(Child, "Father", "target@example.com"), default);
        var invitation = Assert.Single(fixture.Invitations.Items);
        Assert.True(result.IsSuccess);
        Assert.Equal("raw-token", result.Value.InvitationToken);
        Assert.NotEqual("raw-token", invitation.TokenHash);
        Assert.DoesNotContain("raw-token", invitation.TokenHash);
    }

    [Fact]
    public async Task Duplicate_pending_invitation_is_rejected()
    {
        var fixture = Fixture.ForRequester();
        await fixture.CreateInvitationHandler().Handle(
            new(Child, "Father", "target@example.com"), default);
        var second = await fixture.CreateInvitationHandler().Handle(
            new(Child, "Father", "target@example.com"), default);
        Assert.Equal("GENEALOGY_INVITATION_ALREADY_PENDING", second.Error.Code);
    }

    [Fact]
    public async Task Invitation_context_exposes_only_required_child_context()
    {
        var fixture = Fixture.WithPendingInvitation();
        fixture.AsTarget();
        var result = await fixture.GetInvitationHandler().Handle(new("raw-token"), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(Child, result.Value.ChildPetId);
        Assert.Equal("Puppy", result.Value.ChildPetName);
        Assert.Equal("Requester", result.Value.RequesterDisplayName);
    }

    [Fact]
    public async Task Target_accepts_with_own_pet()
    {
        var fixture = Fixture.WithPendingInvitation();
        fixture.AsTarget();
        var result = await fixture.AcceptHandler().Handle(new("raw-token", Father), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(RelationshipInvitationStatus.Accepted, fixture.Invitations.Items.Single().Status);
        Assert.Single(fixture.Relationships.Items);
    }

    [Fact]
    public async Task Target_cannot_accept_with_foreign_pet()
    {
        var fixture = Fixture.WithPendingInvitation();
        fixture.AsTarget();
        var result = await fixture.AcceptHandler().Handle(new("raw-token", Mother), default);
        Assert.Equal("GENEALOGY_FORBIDDEN", result.Error.Code);
    }

    [Fact]
    public async Task Expired_invitation_is_rejected_and_marked_expired()
    {
        var fixture = Fixture.WithPendingInvitation(expiresAt: Now.AddMinutes(-1));
        fixture.AsTarget();
        var result = await fixture.AcceptHandler().Handle(new("raw-token", Father), default);
        Assert.Equal("GENEALOGY_INVITATION_EXPIRED", result.Error.Code);
        Assert.Equal(RelationshipInvitationStatus.Expired, fixture.Invitations.Items.Single().Status);
    }

    [Fact]
    public async Task Rejected_invitation_cannot_be_accepted()
    {
        var fixture = Fixture.WithPendingInvitation();
        fixture.AsTarget();
        await fixture.RejectHandler().Handle(new("raw-token"), default);
        var result = await fixture.AcceptHandler().Handle(new("raw-token", Father), default);
        Assert.Equal("GENEALOGY_INVITATION_ALREADY_PROCESSED", result.Error.Code);
    }

    [Fact]
    public async Task Cancelled_invitation_cannot_be_accepted()
    {
        var fixture = Fixture.WithPendingInvitation();
        var invitation = fixture.Invitations.Items.Single();
        await fixture.CancelHandler().Handle(new(invitation.Id), default);
        fixture.AsTarget();
        var result = await fixture.AcceptHandler().Handle(new("raw-token", Father), default);
        Assert.Equal("GENEALOGY_INVITATION_ALREADY_PROCESSED", result.Error.Code);
    }

    [Fact]
    public async Task User_not_matching_target_email_cannot_view_invitation()
    {
        var fixture = Fixture.WithPendingInvitation();
        fixture.User.EmailValue = "stranger@example.com";
        var result = await fixture.GetInvitationHandler().Handle(new("raw-token"), default);
        Assert.Equal("GENEALOGY_FORBIDDEN", result.Error.Code);
    }

    private sealed class Fixture
    {
        public FakeRelationshipRepository Relationships { get; } = new();
        public FakeInvitationRepository Invitations { get; } = new();
        public FakePetService Pets { get; } = new();
        public FakeCurrentUser User { get; } = new(Requester, "requester@example.com", "Requester");
        public MutableTimeProvider Time { get; } = new(Now);
        public FakeUnitOfWork Unit { get; } = new();
        public TestTokenService Tokens { get; } = new();

        public static Fixture ForRequester()
        {
            var fixture = new Fixture();
            fixture.Pets.Add(new(Child, Requester, "Puppy", 1, "Mixed", "M", Now.AddYears(-1), null));
            fixture.Pets.Add(new(Father, Requester, "Dad", 1, "Mixed", "M", Now.AddYears(-3), null));
            fixture.Pets.Add(new(Mother, Requester, "Mom", 1, "Mixed", "F", Now.AddYears(-3), null));
            return fixture;
        }

        public static Fixture WithPendingInvitation(DateTime? expiresAt = null)
        {
            var fixture = ForRequester();
            fixture.Pets.Items[Father] = fixture.Pets.Items[Father] with { OwnerUserId = Target };
            var invitation = RelationshipInvitation.Create(Child, ParentRole.Father, Requester,
                "Requester", "target@example.com", new TestTokenService().HashToken("raw-token"),
                expiresAt ?? Now.AddHours(72), Now);
            fixture.Invitations.Items.Add(invitation);
            return fixture;
        }

        public void AsTarget()
        {
            User.UserIdValue = Target;
            User.EmailValue = "target@example.com";
            User.DisplayNameValue = "Target";
        }

        public AddOwnParentCommandHandler AddHandler() =>
            new(Relationships, Unit, Pets, User, Time);
        public DeleteRelationshipCommandHandler DeleteHandler() =>
            new(Relationships, Unit, Pets, User, Time);
        public GetRelationshipTreeQueryHandler TreeHandler() => new(Relationships, Pets, User);
        public CreateInvitationCommandHandler CreateInvitationHandler() => new(
            Invitations, Relationships, Unit, Pets, Tokens, new NullEmail(), User, Time,
            Options.Create(new GenealogyInvitationOptions()),
            NullLogger<CreateInvitationCommandHandler>.Instance);
        public GetInvitationQueryHandler GetInvitationHandler() =>
            new(Invitations, Unit, Pets, Tokens, User, Time);
        public AcceptInvitationCommandHandler AcceptHandler() => new(
            Invitations, Relationships, Unit, Pets, Tokens, new NullNotifications(),
            User, Time, NullLogger<AcceptInvitationCommandHandler>.Instance);
        public RejectInvitationCommandHandler RejectHandler() => new(
            Invitations, Unit, Tokens, new NullNotifications(), User, Time,
            NullLogger<RejectInvitationCommandHandler>.Instance);
        public CancelInvitationCommandHandler CancelHandler() =>
            new(Invitations, Unit, User, Time);
    }

    private sealed class FakeCurrentUser(Guid id, string email, string displayName) : ICurrentUser
    {
        public Guid UserIdValue { get; set; } = id;
        public string EmailValue { get; set; } = email;
        public string DisplayNameValue { get; set; } = displayName;
        public Guid UserId => UserIdValue;
        public string Email => EmailValue;
        public string DisplayName => DisplayNameValue;
        public bool IsAuthenticated => true;
    }

    private sealed class FakePetService : IGenealogyPetService
    {
        public Dictionary<Guid, GenealogyPetContext> Items { get; } = [];
        public void Add(GenealogyPetContext pet) => Items[pet.PetId] = pet;
        public Task<GenealogyPetContext?> GetOwnedPetAsync(Guid petId, Guid ownerUserId,
            CancellationToken cancellationToken = default) => Task.FromResult(
            Items.TryGetValue(petId, out var pet) && pet.OwnerUserId == ownerUserId ? pet : null);
        public Task<IReadOnlyDictionary<Guid, GenealogyPetContext>> GetPetContextsAsync(
            IReadOnlyCollection<Guid> petIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, GenealogyPetContext>>(
                Items.Where(pair => petIds.Contains(pair.Key)).ToDictionary());
    }

    private sealed class FakeRelationshipRepository : IPetRelationshipRepository
    {
        public List<PetRelationship> Items { get; } = [];
        public Task<PetRelationship?> GetByIdAsync(Guid relationshipId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(item => item.Id == relationshipId));
        public Task<PetRelationship?> GetActiveForChildRoleAsync(Guid childPetId, ParentRole role,
            CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(
                item => item.ChildPetId == childPetId && item.ParentRole == role && item.IsActive));
        public Task<IReadOnlyList<PetRelationship>> GetActiveGraphAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PetRelationship>>(Items.Where(item => item.IsActive).ToArray());
        public Task AddAsync(PetRelationship relationship, CancellationToken cancellationToken = default)
        { Items.Add(relationship); return Task.CompletedTask; }
    }

    private sealed class FakeInvitationRepository : IRelationshipInvitationRepository
    {
        public List<RelationshipInvitation> Items { get; } = [];
        public Task<RelationshipInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(item => item.TokenHash == tokenHash));
        public Task<RelationshipInvitation?> GetByIdAsync(Guid invitationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(item => item.Id == invitationId));
        public Task<bool> HasPendingEquivalentAsync(Guid childPetId, ParentRole role, string targetEmail,
            CancellationToken cancellationToken = default) => Task.FromResult(Items.Any(item =>
                item.ChildPetId == childPetId && item.ParentRole == role && item.TargetEmail == targetEmail &&
                item.Status == RelationshipInvitationStatus.Pending));
        public Task<IReadOnlyList<RelationshipInvitation>> GetMineAsync(Guid userId, string email,
            RelationshipInvitationStatus? status, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RelationshipInvitation>>(Items.Where(item =>
                (item.RequesterUserId == userId || item.IsForEmail(email)) &&
                (!status.HasValue || item.Status == status)).ToArray());
        public Task AddAsync(RelationshipInvitation invitation, CancellationToken cancellationToken = default)
        { Items.Add(invitation); return Task.CompletedTask; }
    }

    private sealed class FakeUnitOfWork : IGenealogyUnitOfWork
    { public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class MutableTimeProvider(DateTime now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => new(now); }
    private sealed class TestTokenService : IInvitationTokenService
    {
        public string GenerateToken() => "raw-token";
        public string HashToken(string token) => Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
    private sealed class NullEmail : IGenealogyInvitationEmailSender
    { public Task SendAsync(RelationshipInvitation invitation, string token, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class NullNotifications : IGenealogyNotificationPublisher
    { public Task PublishAsync(string eventType, Guid userId, Guid invitationId, CancellationToken cancellationToken = default) => Task.CompletedTask; }
}
