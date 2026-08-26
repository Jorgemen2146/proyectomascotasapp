using DogPlatform.Matching.Application.Features.Matches;
using DogPlatform.Matching.Domain.Aggregates.BreedingIntent;
using DogPlatform.Matching.Domain.Aggregates.PetMatch;
using DogPlatform.Matching.Domain.Enums;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Matching.Tests;

public sealed partial class MatchingMvpTests
{
    [Fact]
    public async Task BreedingIntent_Create_PersistsProposedAndNotifiesOtherOwner()
    {
        var match = AcceptedMatch();
        var intents = new FakeBreedingIntentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var notifications = new FakeNotifications();
        var handler = new ProposeBreedingIntentCommandHandler(
            new FakeMatchRepository(match), intents, unitOfWork,
            new CurrentUser(Owner1), new FixedTime(), notifications);

        var result = await handler.Handle(
            new ProposeBreedingIntentCommand(match.Id, "Possible litter", Now.AddMonths(2)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(BreedingIntentStatus.Proposed.ToString(), result.Value.Status);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal(result.Value.BreedingIntentId, Assert.Single(intents.Items).Id);
        var notification = Assert.Single(notifications.Items);
        Assert.Equal(Owner2, notification.UserId);
        Assert.Equal("MatchingBreedingIntentProposed", notification.Type);
        Assert.Equal(match.Id, notification.Metadata?.MatchId);
        Assert.Equal(result.Value.BreedingIntentId, notification.Metadata?.BreedingIntentId);
    }

    [Fact]
    public async Task GetBreedingIntent_ReturnsPersistedProposed()
    {
        var match = AcceptedMatch();
        var intent = Intent(match.Id);
        var result = await GetIntent(match, intent, Owner1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Proposed", result.Value.Status);
        Assert.Equal(intent.Id, result.Value.BreedingIntentId);
    }

    [Fact]
    public async Task MatchDetail_ReflectsLatestBreedingIntent()
    {
        var match = AcceptedMatch();
        var intent = Intent(match.Id);

        var result = await Detail(match, Owner1, intent);

        Assert.True(result.IsSuccess);
        Assert.Equal(intent.Id, result.Value.BreedingIntent?.BreedingIntentId);
        Assert.Equal("Proposed", result.Value.BreedingIntent?.Status);
    }

    [Fact]
    public async Task Proposer_SeesProposedByCurrentUserTrue()
    {
        var match = AcceptedMatch();
        var result = await GetIntent(match, Intent(match.Id), Owner1);
        Assert.True(result.Value.ProposedByCurrentUser);
    }

    [Fact]
    public async Task OtherOwner_SeesProposedByCurrentUserFalse()
    {
        var match = AcceptedMatch();
        var result = await GetIntent(match, Intent(match.Id), Owner2);
        Assert.False(result.Value.ProposedByCurrentUser);
    }

    [Fact]
    public async Task Outsider_CannotGetBreedingIntent()
    {
        var match = AcceptedMatch();
        var result = await GetIntent(match, Intent(match.Id), Guid.NewGuid());
        Assert.Equal("MATCHING_FORBIDDEN", result.Error.Code);
    }

    [Fact]
    public async Task SecondOpenBreedingIntent_IsBlocked()
    {
        var match = AcceptedMatch();
        var handler = new ProposeBreedingIntentCommandHandler(
            new FakeMatchRepository(match), new FakeBreedingIntentRepository(Intent(match.Id)),
            new FakeUnitOfWork(), new CurrentUser(Owner2), new FixedTime(), new FakeNotifications());

        var result = await handler.Handle(
            new ProposeBreedingIntentCommand(match.Id, null, null), default);

        Assert.Equal("MATCHING_BREEDING_INTENT_EXISTS", result.Error.Code);
    }

    [Fact]
    public void Proposer_CannotAcceptOwnBreedingIntent()
    {
        var intent = Intent(Guid.NewGuid());
        var result = intent.Accept(Owner1, Owner1, Owner2, Now);
        Assert.Equal("MATCHING_FORBIDDEN", result.Error.Code);
    }

    [Fact]
    public async Task Accept_ChangesToAgreedAndNotifiesProposer()
    {
        var match = AcceptedMatch();
        var intent = Intent(match.Id);
        var notifications = new FakeNotifications();
        var handler = new AcceptBreedingIntentCommandHandler(
            new FakeMatchRepository(match), new FakeBreedingIntentRepository(intent),
            new FakeUnitOfWork(), new CurrentUser(Owner2), new FixedTime(), notifications);

        var result = await handler.Handle(new AcceptBreedingIntentCommand(intent.Id), default);

        Assert.Equal("Agreed", result.Value.Status);
        var notification = Assert.Single(notifications.Items);
        Assert.Equal(Owner1, notification.UserId);
        Assert.Equal("MatchingBreedingIntentAccepted", notification.Type);
        Assert.Equal(match.Id, notification.Metadata?.MatchId);
        Assert.Equal(intent.Id, notification.Metadata?.BreedingIntentId);
    }

    [Fact]
    public async Task RefreshGet_StillReturnsAgreed()
    {
        var match = AcceptedMatch();
        var intent = Intent(match.Id);
        Assert.True(intent.Accept(Owner2, Owner1, Owner2, Now).IsSuccess);

        var result = await GetIntent(match, intent, Owner1);

        Assert.Equal("Agreed", result.Value.Status);
    }

    [Fact]
    public async Task Cancel_ChangesToCancelledAndNotifiesOtherOwner()
    {
        var match = AcceptedMatch();
        var intent = Intent(match.Id);
        var notifications = new FakeNotifications();
        var handler = new CancelBreedingIntentCommandHandler(
            new FakeMatchRepository(match), new FakeBreedingIntentRepository(intent),
            new FakeUnitOfWork(), new CurrentUser(Owner1), new FixedTime(), notifications);

        var result = await handler.Handle(new CancelBreedingIntentCommand(intent.Id), default);

        Assert.Equal("Cancelled", result.Value.Status);
        Assert.Null(intent.OpenMatchId);
        Assert.Equal("MatchingBreedingIntentCancelled", Assert.Single(notifications.Items).Type);
    }

    [Fact]
    public void CancelledBreedingIntent_CannotBeAccepted()
    {
        var intent = Intent(Guid.NewGuid());
        Assert.True(intent.Cancel(Owner1, Owner1, Owner2, Now).IsSuccess);
        Assert.Equal("MATCHING_REQUEST_ALREADY_PROCESSED",
            intent.Accept(Owner2, Owner1, Owner2, Now).Error.Code);
    }

    [Fact]
    public async Task AfterCancelled_NewProposalCanBeCreated()
    {
        var match = AcceptedMatch();
        var cancelled = Intent(match.Id);
        Assert.True(cancelled.Cancel(Owner1, Owner1, Owner2, Now).IsSuccess);
        var intents = new FakeBreedingIntentRepository(cancelled);
        var handler = new ProposeBreedingIntentCommandHandler(
            new FakeMatchRepository(match), intents, new FakeUnitOfWork(),
            new CurrentUser(Owner2), new FixedTime(), new FakeNotifications());

        var result = await handler.Handle(
            new ProposeBreedingIntentCommand(match.Id, "New proposal", null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, intents.Items.Count);
    }

    [Fact]
    public void NonProposer_CannotCancelProposedIntent()
    {
        var intent = Intent(Guid.NewGuid());
        Assert.Equal("MATCHING_FORBIDDEN",
            intent.Cancel(Owner2, Owner1, Owner2, Now).Error.Code);
    }

    [Fact]
    public async Task ConcurrentUniqueViolation_MapsToStableConflict()
    {
        var match = AcceptedMatch();
        var handler = new ProposeBreedingIntentCommandHandler(
            new FakeMatchRepository(match), new FakeBreedingIntentRepository(),
            new FakeUnitOfWork(new BreedingIntentConflictException(new Exception("duplicate"))),
            new CurrentUser(Owner1), new FixedTime(), new FakeNotifications());

        var result = await handler.Handle(
            new ProposeBreedingIntentCommand(match.Id, null, null), default);

        Assert.Equal("MATCHING_BREEDING_INTENT_EXISTS", result.Error.Code);
    }

    [Fact]
    public void EfModel_HasUniqueOpenMatchConstraint()
    {
        var options = new DbContextOptionsBuilder<MatchingDbContext>()
            .UseSqlServer("Server=(local);Database=unused;Trusted_Connection=True")
            .Options;
        using var context = new MatchingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(BreedingIntent));
        var index = Assert.Single(entity!.GetIndexes(), candidate =>
            candidate.Properties.Count == 1
            && candidate.Properties[0].Name == nameof(BreedingIntent.OpenMatchId));

        Assert.True(index.IsUnique);
        Assert.Equal("[OpenMatchId] IS NOT NULL", index.GetFilter());
    }

    [Fact]
    public void BreedingIntentContract_DoesNotExposeOwnerUserIds()
    {
        var properties = typeof(BreedingIntentResponse).GetProperties().Select(property => property.Name);
        Assert.DoesNotContain(properties, name => name.Contains("Owner", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, name => name.Contains("UserId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BreedingNotificationMetadata_ContainsNoPrivateContact()
    {
        var properties = typeof(DogPlatform.Matching.Application.Clients.Notifications.MatchingNotificationMetadata)
            .GetProperties().Select(property => property.Name).ToArray();
        Assert.Equal(["MatchId", "BreedingIntentId"], properties);
        Assert.DoesNotContain(properties, name => name.Contains("Phone", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, name => name.Contains("Email", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, name => name.Contains("User", StringComparison.OrdinalIgnoreCase));
    }

    private static PetMatch AcceptedMatch() =>
        PetMatch.Create(Guid.NewGuid(), Pet1, Pet2, Owner1, Owner2, false, false, Now);

    private static BreedingIntent Intent(Guid matchId) =>
        BreedingIntent.Create(matchId, Owner1, "Possible litter", Now.AddMonths(2), Now).Value;

    private static Task<DogPlatform.SharedKernel.Primitives.Result<BreedingIntentResponse>> GetIntent(
        PetMatch match, BreedingIntent intent, Guid userId) =>
        new GetBreedingIntentQueryHandler(new FakeMatchRepository(match),
            new FakeBreedingIntentRepository(intent), new CurrentUser(userId))
            .Handle(new GetBreedingIntentQuery(match.Id), default);
}
