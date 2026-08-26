using DogPlatform.Matching.Application.Clients.Genealogy;
using DogPlatform.Matching.Application.Clients.Health;
using DogPlatform.Matching.Application.Clients.Identity;
using DogPlatform.Matching.Application.Clients.Notifications;
using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Evaluation;
using DogPlatform.Matching.Application.Features.CreateMatchRequest;
using DogPlatform.Matching.Application.Features.Matches;
using DogPlatform.Matching.Application.Features.SearchCandidates;
using DogPlatform.Matching.Application.Options;
using DogPlatform.Matching.Application.Scoring;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Aggregates.BreedingIntent;
using DogPlatform.Matching.Domain.Aggregates.MatchRequest;
using DogPlatform.Matching.Domain.Aggregates.MatchingProfile;
using DogPlatform.Matching.Domain.Aggregates.PetMatch;
using DogPlatform.Matching.Domain.Enums;
using DogPlatform.Matching.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace DogPlatform.Matching.Tests;

public sealed partial class MatchingMvpTests
{
    private static readonly DateTime Now = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Owner2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Pet1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Pet2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Search_DoesNotIncludeSameOwner()
    {
        var evaluation = await Evaluation().EvaluateAsync(Pet(Pet1, Owner1, "M"),
            Pet(Pet2, Owner1, "F"), Profile(Pet1, Owner1), default);
        Assert.True(evaluation.IsExcluded);
        Assert.Equal("SameOwner", evaluation.ExclusionReason);
    }

    [Fact]
    public void SearchResponse_DoesNotExposeOwnerOrContact()
    {
        var names = typeof(CandidateSummaryResponse).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain(names, name => name.Contains("Owner", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("Phone", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("Email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PublicDetail_DoesNotExposeContact()
    {
        var names = typeof(PublicMatchingPet).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain(names, name => name.Contains("Owner", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("Contact", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Request_Create_HasPendingStatus()
    {
        var result = Request();
        Assert.True(result.IsSuccess);
        Assert.Equal(MatchRequestStatus.Pending, result.Value.Status);
    }

    [Fact]
    public async Task Request_DuplicatePending_IsBlocked()
    {
        var fixture = new CreateFixture { HasActiveRequest = true };
        var result = await fixture.Handle();
        Assert.Equal("MATCHING_REQUEST_EXISTS", result.Error.Code);
    }

    [Fact]
    public async Task RequesterPet_MustBelongToCurrentUser()
    {
        var fixture = new CreateFixture { CurrentUserId = Guid.NewGuid() };
        var result = await fixture.Handle();
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void OnlyTargetOwner_CanAcceptRequest()
    {
        var request = Request().Value;
        var result = request.Accept(Guid.NewGuid(), Now);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void SelfRequest_IsBlocked()
    {
        var result = MatchRequest.Create(Pet1, Owner1, Pet1, Owner1, null,
            100, 0, RelationshipTypeSnapshot.UnrelatedWithinKnownPedigree, false, Now);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void TargetOwner_CanAccept()
    {
        var request = Request().Value;
        Assert.True(request.Accept(Owner2, Now).IsSuccess);
        Assert.Equal(MatchRequestStatus.Accepted, request.Status);
    }

    [Fact]
    public void TargetOwner_CanReject()
    {
        var request = Request().Value;
        Assert.True(request.Reject(Owner2, Now).IsSuccess);
        Assert.Equal(MatchRequestStatus.Rejected, request.Status);
    }

    [Fact]
    public void Requester_CanCancelPendingRequest()
    {
        var request = Request().Value;
        Assert.True(request.Cancel(Owner1, Now).IsSuccess);
        Assert.Equal(MatchRequestStatus.Cancelled, request.Status);
    }

    [Fact]
    public async Task Contact_IsHiddenWhileRequestIsPending()
    {
        var handler = new GetMatchDetailQueryHandler(new FakeMatchRepository(),
            new FakeBreedingIntentRepository(),
            new FakePetsClient(), new FakeIdentityClient(), new CurrentUser(Owner1));
        var result = await handler.Handle(new GetMatchDetailQuery(Guid.NewGuid()), default);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Contact_IsVisibleAfterAcceptedMatch()
    {
        var match = PetMatch.Create(Guid.NewGuid(), Pet1, Pet2, Owner1, Owner2, true, true, Now);
        var result = await Detail(match, Owner1);
        Assert.True(result.IsSuccess);
        Assert.Equal("Owner One", result.Value.Pet1Owner.DisplayName);
        Assert.Equal("111", result.Value.Pet1Owner.PhoneNumber);
    }

    [Fact]
    public async Task Contact_ReturnsOnlyExplicitlySharedPhoneNumbers()
    {
        var match = PetMatch.Create(Guid.NewGuid(), Pet1, Pet2, Owner1, Owner2, true, false, Now);
        var result = await Detail(match, Owner1);
        Assert.NotNull(result.Value.Pet1Owner.PhoneNumber);
        Assert.Null(result.Value.Pet2Owner.PhoneNumber);
    }

    [Fact]
    public async Task Outsider_CannotReadAcceptedMatch()
    {
        var match = PetMatch.Create(Guid.NewGuid(), Pet1, Pet2, Owner1, Owner2, false, false, Now);
        var result = await Detail(match, Guid.NewGuid());
        Assert.Equal("MATCHING_FORBIDDEN", result.Error.Code);
    }

    [Fact]
    public async Task Notification_BeforeAcceptance_ContainsNoOwnerContact()
    {
        var fixture = new CreateFixture();
        var result = await fixture.Handle();
        Assert.True(result.IsSuccess);
        var notification = Assert.Single(fixture.Notifications.Items);
        Assert.DoesNotContain("@", notification.Message);
        Assert.DoesNotContain("111", notification.Message);
        Assert.Null(typeof(MatchingNotification).GetProperty("OwnerEmail"));
    }

    [Fact]
    public async Task GenealogyRelatedCandidate_IsMarkedAndWarned()
    {
        var evaluation = await Evaluation(RelationshipTypeSnapshot.FirstCousin,
            ["Known related pets"]).EvaluateAsync(Pet(Pet1, Owner1, "M"),
            Pet(Pet2, Owner2, "F"), Profile(Pet1, Owner1), default);
        Assert.False(evaluation.IsExcluded);
        Assert.Equal(RelationshipTypeSnapshot.FirstCousin, evaluation.RelationshipType);
        Assert.Contains("Known related pets", evaluation.Warnings);
    }

    [Fact]
    public void BreedingIntent_StartsProposed()
    {
        var result = BreedingIntent.Create(Guid.NewGuid(), Owner1, "Possible litter", null, Now);
        Assert.Equal(BreedingIntentStatus.Proposed, result.Value.Status);
    }

    [Fact]
    public void OtherOwner_CanAcceptBreedingIntent()
    {
        var intent = BreedingIntent.Create(Guid.NewGuid(), Owner1, null, null, Now).Value;
        Assert.True(intent.Accept(Owner2, Owner1, Owner2, Now).IsSuccess);
        Assert.Equal(BreedingIntentStatus.Agreed, intent.Status);
    }

    [Fact]
    public void MatchOwner_CanCancelBreedingIntent()
    {
        var intent = BreedingIntent.Create(Guid.NewGuid(), Owner1, null, null, Now).Value;
        Assert.True(intent.Cancel(Owner1, Owner1, Owner2, Now).IsSuccess);
        Assert.Equal(BreedingIntentStatus.Cancelled, intent.Status);
    }

    private static DogPlatform.SharedKernel.Primitives.Result<MatchRequest> Request() =>
        MatchRequest.Create(Pet1, Owner1, Pet2, Owner2, "Hello", 100, 0,
            RelationshipTypeSnapshot.UnrelatedWithinKnownPedigree, false, Now);

    private static MatchingProfile Profile(Guid petId, Guid ownerId) =>
        MatchingProfile.Create(petId, ownerId, true, [], 12, 120, false,
            false, 1, 0, Now, "F", true).Value;

    private static PetMatchingDataResponse Pet(Guid petId, Guid ownerId, string sex) =>
        new(petId, ownerId, petId == Pet1 ? "Rex" : "Luna", 1, "Breed", sex,
            30, null, false, true, 1, "Dog", null, "Brown", false);

    private static CandidateEvaluationService Evaluation(
        RelationshipTypeSnapshot relationship = RelationshipTypeSnapshot.UnrelatedWithinKnownPedigree,
        IReadOnlyList<string>? warnings = null)
    {
        var options = Options.Create(new MatchingOptions { ExcludedRelationshipTypes = [] });
        return new CandidateEvaluationService(new FakeGenealogy(relationship, warnings ?? []),
            new FakeHealth(), new MatchScoringService(options), options);
    }

    private static async Task<DogPlatform.SharedKernel.Primitives.Result<PetMatchDetailResponse>> Detail(
        PetMatch match, Guid userId, params BreedingIntent[] intents)
    {
        var handler = new GetMatchDetailQueryHandler(new FakeMatchRepository(match),
            new FakeBreedingIntentRepository(intents),
            new FakePetsClient(Pet(Pet1, Owner1, "M"), Pet(Pet2, Owner2, "F")),
            new FakeIdentityClient(), new CurrentUser(userId));
        return await handler.Handle(new GetMatchDetailQuery(match.Id), default);
    }

    private sealed class CreateFixture
    {
        public bool HasActiveRequest { get; init; }
        public Guid CurrentUserId { get; init; } = Owner1;
        public FakeNotifications Notifications { get; } = new();
        public async Task<DogPlatform.SharedKernel.Primitives.Result<DogPlatform.Matching.Application.Features.Common.MatchRequestResponse>> Handle()
        {
            var profiles = new FakeProfileRepository(Profile(Pet1, Owner1), Profile(Pet2, Owner2));
            var requests = new FakeRequestRepository { HasActive = HasActiveRequest };
            var handler = new CreateMatchRequestCommandHandler(profiles, requests,
                new FakeUnitOfWork(), new FakePetsClient(Pet(Pet1, Owner1, "M"), Pet(Pet2, Owner2, "F")),
                Evaluation(), new CurrentUser(CurrentUserId), new FixedTime(), Notifications);
            return await handler.Handle(new CreateMatchRequestCommand(Pet1, Pet2, "Hello"), default);
        }
    }

    private sealed record CurrentUser(Guid UserId) : ICurrentUser { public bool IsAuthenticated => true; }
    private sealed class FixedTime : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero); }
    private sealed class FakeUnitOfWork(Exception? exception = null) : IMatchingUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return exception is null ? Task.CompletedTask : Task.FromException(exception);
        }
    }

    private sealed class FakeProfileRepository(params MatchingProfile[] profiles) : IMatchingProfileRepository
    {
        public Task<MatchingProfile?> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken = default) => Task.FromResult(profiles.FirstOrDefault(x => x.PetId == petId));
        public Task<MatchingProfile?> GetActiveByPetIdAsync(Guid petId, CancellationToken cancellationToken = default) => Task.FromResult(profiles.FirstOrDefault(x => x.PetId == petId && x.IsActive));
        public Task<IReadOnlyList<MatchingProfile>> GetActiveByPetIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) { var set=ids.ToHashSet(); return Task.FromResult<IReadOnlyList<MatchingProfile>>(profiles.Where(x => x.IsActive && set.Contains(x.PetId)).ToList()); }
        public Task<MatchingProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(profiles.FirstOrDefault(x => x.Id == id));
        public void Add(MatchingProfile profile) { }
        public void Update(MatchingProfile profile) { }
    }

    private sealed class FakeRequestRepository : IMatchRequestRepository
    {
        public bool HasActive { get; init; }
        public List<MatchRequest> Items { get; } = [];
        public Task<MatchRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<bool> HasActiveRequestAsync(Guid requesterPetId, Guid candidatePetId, CancellationToken cancellationToken = default) => Task.FromResult(HasActive);
        public Task<(IReadOnlyCollection<MatchRequest> Items, int TotalItems)> GetIncomingAsync(Guid ownerId, MatchRequestStatus? status, int page, int size, CancellationToken cancellationToken = default) => Task.FromResult(((IReadOnlyCollection<MatchRequest>)Items, Items.Count));
        public Task<(IReadOnlyCollection<MatchRequest> Items, int TotalItems)> GetOutgoingAsync(Guid ownerId, MatchRequestStatus? status, int page, int size, CancellationToken cancellationToken = default) => Task.FromResult(((IReadOnlyCollection<MatchRequest>)Items, Items.Count));
        public void Add(MatchRequest request) => Items.Add(request);
        public void Update(MatchRequest request) { }
    }

    private sealed class FakePetsClient(params PetMatchingDataResponse[] pets) : IPetsMatchingClient
    {
        public Task<PetMatchingDataResponse?> GetPetForMatchingAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(pets.FirstOrDefault(x => x.PetId == id));
        public Task<CandidateSearchPage?> SearchCandidatesAsync(CandidateSearchFilter filter, CancellationToken cancellationToken = default) => Task.FromResult<CandidateSearchPage?>(null);
        public Task<IReadOnlyCollection<PetMatchingDataResponse>> GetPetsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<PetMatchingDataResponse>>(pets.Where(x => ids.Contains(x.PetId)).ToList());
        public Task<bool> VerifyOwnershipAsync(Guid petId, Guid ownerId, CancellationToken cancellationToken = default) => Task.FromResult(pets.Any(x => x.PetId == petId && x.OwnerId == ownerId));
    }

    private sealed class FakeGenealogy(RelationshipTypeSnapshot relationship, IReadOnlyList<string> warnings) : IGenealogyMatchingClient
    {
        public Task<RelationshipEvaluationResult?> CalculateRelationshipAsync(Guid a, Guid b, CancellationToken cancellationToken = default) => Task.FromResult<RelationshipEvaluationResult?>(new(relationship, relationship != RelationshipTypeSnapshot.UnrelatedWithinKnownPedigree, 0, GenealogyValidationStatus.Validated, warnings));
        public Task<OffspringInbreedingEstimate?> EstimateOffspringInbreedingAsync(Guid a, Guid b, CancellationToken cancellationToken = default) => Task.FromResult<OffspringInbreedingEstimate?>(new(0, GenealogyValidationStatus.Validated, []));
        public Task<PedigreeStatisticsSummary?> GetPedigreeStatisticsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PedigreeStatisticsSummary?>(new(100, GenealogyValidationStatus.Validated, []));
    }
    private sealed class FakeHealth : IHealthMatchingClient { public Task<HealthCompatibilityResult> EvaluateAsync(Guid a, Guid b, CancellationToken cancellationToken = default) => Task.FromResult(new HealthCompatibilityResult(HealthCompatibilityStatus.Unknown, [], Now)); }
    private sealed class FakeNotifications : IMatchingNotificationClient { public List<MatchingNotification> Items { get; }=[]; public Task SendAsync(MatchingNotification notification, CancellationToken cancellationToken=default) { Items.Add(notification); return Task.CompletedTask; } }
    private sealed class FakeIdentityClient : IIdentityMatchingClient { public Task<MatchingOwnerContact?> GetMatchingContactAsync(Guid id, CancellationToken cancellationToken=default) => Task.FromResult<MatchingOwnerContact?>(id == Owner1 ? new("Owner One", "111") : new("Owner Two", "222")); }
    private sealed class FakeMatchRepository(params PetMatch[] matches) : IPetMatchRepository
    {
        public Task<PetMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken=default) => Task.FromResult(matches.FirstOrDefault(x=>x.Id==id));
        public Task<PetMatch?> GetByRequestIdAsync(Guid id, CancellationToken cancellationToken=default) => Task.FromResult(matches.FirstOrDefault(x=>x.MatchRequestId==id));
        public Task<IReadOnlyList<PetMatch>> GetByOwnerIdAsync(Guid id, CancellationToken cancellationToken=default) => Task.FromResult<IReadOnlyList<PetMatch>>(matches.Where(x=>x.Involves(id)).ToList());
        public void Add(PetMatch match) { }
    }

    private sealed class FakeBreedingIntentRepository(params BreedingIntent[] intents)
        : IBreedingIntentRepository
    {
        public List<BreedingIntent> Items { get; } = [.. intents];
        public Task<BreedingIntent?> GetByIdAsync(Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(intent => intent.Id == id));
        public Task<BreedingIntent?> GetLatestByMatchIdAsync(Guid matchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(intent => intent.MatchId == matchId)
                .OrderByDescending(intent => intent.CreatedAtUtc).FirstOrDefault());
        public Task<bool> HasOpenIntentAsync(Guid matchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(intent => intent.OpenMatchId == matchId));
        public void Add(BreedingIntent intent) => Items.Add(intent);
        public void Update(BreedingIntent intent) { }
    }
}
