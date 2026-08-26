using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Common;
using DogPlatform.Matching.Application.Evaluation;
using DogPlatform.Matching.Application.Features.SearchCandidates;
using DogPlatform.Matching.Application.Options;
using DogPlatform.Matching.Application.Scoring;
using DogPlatform.Matching.Domain.Aggregates.FavoriteCandidate;
using DogPlatform.Matching.Domain.Enums;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace DogPlatform.Matching.Tests;

public sealed partial class MatchingMvpTests
{
    [Fact]
    public async Task Search_PedigreeOmitted_ReturnsAllCandidates()
    {
        var fixture = SearchFixture(Candidate(1, "PED-1"), Candidate(2, null));
        var query = new SearchCandidatesQuery(Pet1, 1, 10, null, null, null,
            null, "CompatibilityScore", "DESC", false);

        var result = await fixture.Handler.Handle(query, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalItems);
    }

    [Fact]
    public async Task Search_PedigreeAny_ReturnsAllCandidates()
    {
        var fixture = SearchFixture(Candidate(1, "PED-1"), Candidate(2, null));

        var result = await fixture.Search(pedigree: PedigreeFilter.Any);

        Assert.Equal(2, result.Value.TotalItems);
        Assert.Contains(result.Value.Items, item => item.HasPedigree);
        Assert.Contains(result.Value.Items, item => !item.HasPedigree);
    }

    [Fact]
    public async Task Search_WithPedigree_ReturnsOnlyCandidatesWithNonBlankNumber()
    {
        var fixture = SearchFixture(
            Candidate(1, "PED-1"), Candidate(2, null), Candidate(3, ""), Candidate(4, "   "));

        var result = await fixture.Search(pedigree: PedigreeFilter.WithPedigree);

        var item = Assert.Single(result.Value.Items);
        Assert.True(item.HasPedigree);
        Assert.Equal(1, result.Value.TotalItems);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_WithoutPedigree_IncludesNullEmptyAndWhitespace(string? pedigreeNumber)
    {
        var fixture = SearchFixture(Candidate(1, pedigreeNumber), Candidate(2, "PED-2"));

        var result = await fixture.Search(pedigree: PedigreeFilter.WithoutPedigree);

        var item = Assert.Single(result.Value.Items);
        Assert.False(item.HasPedigree);
        Assert.Equal(1, result.Value.TotalItems);
    }

    [Fact]
    public async Task Search_AgeBoundsOmitted_UsesProfileBounds()
    {
        var fixture = SearchFixture(Candidate(1, null, ageMonths: 20));

        var result = await fixture.Search();

        Assert.True(result.IsSuccess);
        Assert.Equal(12, fixture.Pets.LastFilter!.MinimumAgeMonths);
        Assert.Equal(120, fixture.Pets.LastFilter.MaximumAgeMonths);
    }

    [Fact]
    public async Task Search_OnlyMinimumAge_AppliesMinimumAndKeepsProfileMaximum()
    {
        var fixture = SearchFixture(
            Candidate(1, null, ageMonths: 20), Candidate(2, null, ageMonths: 40));

        var result = await fixture.Search(minimumAgeMonths: 30);

        Assert.Single(result.Value.Items);
        Assert.Equal(30, fixture.Pets.LastFilter!.MinimumAgeMonths);
        Assert.Equal(120, fixture.Pets.LastFilter.MaximumAgeMonths);
    }

    [Fact]
    public async Task Search_OnlyMaximumAge_AppliesMaximumAndKeepsProfileMinimum()
    {
        var fixture = SearchFixture(
            Candidate(1, null, ageMonths: 20), Candidate(2, null, ageMonths: 40));

        var result = await fixture.Search(maximumAgeMonths: 30);

        Assert.Single(result.Value.Items);
        Assert.Equal(12, fixture.Pets.LastFilter!.MinimumAgeMonths);
        Assert.Equal(30, fixture.Pets.LastFilter.MaximumAgeMonths);
    }

    [Fact]
    public async Task Search_PedigreeAndBreed_AppliesBothFilters()
    {
        var fixture = SearchFixture(
            Candidate(1, "PED-1", breedId: 1), Candidate(2, "PED-2", breedId: 2),
            Candidate(3, null, breedId: 2));

        var result = await fixture.Search(
            breedId: 2, pedigree: PedigreeFilter.WithPedigree);

        var item = Assert.Single(result.Value.Items);
        Assert.Equal(2, item.BreedId);
        Assert.True(item.HasPedigree);
    }

    [Fact]
    public async Task Search_PedigreeAndAge_AppliesBothFilters()
    {
        var fixture = SearchFixture(
            Candidate(1, "PED-1", ageMonths: 20), Candidate(2, "PED-2", ageMonths: 40),
            Candidate(3, null, ageMonths: 40));

        var result = await fixture.Search(
            minimumAgeMonths: 30, pedigree: PedigreeFilter.WithPedigree);

        var item = Assert.Single(result.Value.Items);
        Assert.True(item.HasPedigree);
        Assert.Equal(40, item.AgeMonths);
    }

    [Fact]
    public async Task Search_PedigreeFilter_IsAppliedBeforeCountAndPagination()
    {
        var fixture = SearchFixture(
            Candidate(1, "PED-1"), Candidate(2, null), Candidate(3, "PED-3"),
            Candidate(4, ""), Candidate(5, "PED-5"));

        var result = await fixture.Search(
            pageNumber: 2, pageSize: 2, pedigree: PedigreeFilter.WithPedigree);

        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(2, result.Value.TotalPages);
        Assert.Single(result.Value.Items);
        Assert.All(result.Value.Items, item => Assert.True(item.HasPedigree));
    }

    private static SearchFixtureState SearchFixture(params PetMatchingDataResponse[] candidates)
    {
        var source = Pet(Pet1, Owner1, "M");
        var profiles = new[] { Profile(Pet1, Owner1) }
            .Concat(candidates.Select(candidate => Profile(candidate.PetId, candidate.OwnerId)))
            .ToArray();
        var pets = new SearchPetsClient(source, candidates);
        var options = Options.Create(new MatchingOptions
        {
            ExcludedRelationshipTypes = [],
            MaximumCandidatesEvaluatedPerSearch = 100,
            MaximumPageSize = 50
        });
        var handler = new SearchCandidatesQueryHandler(
            new FakeProfileRepository(profiles), new EmptyFavorites(), pets,
            new CandidateEvaluationService(
                new FakeGenealogy(RelationshipTypeSnapshot.UnrelatedWithinKnownPedigree, []),
                new FakeHealth(), new MatchScoringService(options), options),
            new CurrentUser(Owner1), options, new FixedTime());
        return new SearchFixtureState(handler, pets);
    }

    private static PetMatchingDataResponse Candidate(
        int suffix, string? pedigreeNumber, int breedId = 1, int ageMonths = 30) =>
        new(Guid.Parse($"00000000-0000-0000-0000-{suffix:000000000000}"), Owner2,
            $"Candidate {suffix}", breedId, $"Breed {breedId}", "F", ageMonths,
            null, false, true, 1, "Dog", null, null, false, null, pedigreeNumber);

    private sealed record SearchFixtureState(
        SearchCandidatesQueryHandler Handler, SearchPetsClient Pets)
    {
        public Task<Result<PagedResult<CandidateSummaryResponse>>> Search(
            int pageNumber = 1,
            int pageSize = 10,
            int? breedId = null,
            int? minimumAgeMonths = null,
            int? maximumAgeMonths = null,
            PedigreeFilter pedigree = PedigreeFilter.Any) =>
            Handler.Handle(new SearchCandidatesQuery(Pet1, pageNumber, pageSize, breedId,
                minimumAgeMonths, maximumAgeMonths, null, "CompatibilityScore", "DESC", false,
                pedigree), default);
    }

    private sealed class SearchPetsClient(
        PetMatchingDataResponse source, IReadOnlyCollection<PetMatchingDataResponse> candidates)
        : IPetsMatchingClient
    {
        public CandidateSearchFilter? LastFilter { get; private set; }

        public Task<PetMatchingDataResponse?> GetPetForMatchingAsync(
            Guid petId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PetMatchingDataResponse?>(petId == source.PetId ? source : candidates.FirstOrDefault(x => x.PetId == petId));

        public Task<CandidateSearchPage?> SearchCandidatesAsync(
            CandidateSearchFilter filter, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            var filtered = candidates
                .Where(x => x.OwnerId != filter.ExcludeOwnerId)
                .Where(x => filter.RequiredSex is null || x.Sex.Equals(filter.RequiredSex, StringComparison.OrdinalIgnoreCase))
                .Where(x => !filter.BreedId.HasValue || x.BreedId == filter.BreedId)
                .Where(x => x.AgeMonths >= filter.MinimumAgeMonths && x.AgeMonths <= filter.MaximumAgeMonths)
                .ToList();
            return Task.FromResult<CandidateSearchPage?>(new(filtered, filtered.Count));
        }

        public Task<IReadOnlyCollection<PetMatchingDataResponse>> GetPetsByIdsAsync(
            IReadOnlyCollection<Guid> petIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<PetMatchingDataResponse>>(
                candidates.Where(x => petIds.Contains(x.PetId)).ToList());

        public Task<bool> VerifyOwnershipAsync(
            Guid petId, Guid ownerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(source.PetId == petId && source.OwnerId == ownerId);
    }

    private sealed class EmptyFavorites : IFavoriteCandidateRepository
    {
        public Task<FavoriteCandidate?> GetAsync(Guid sourcePetId, Guid candidatePetId,
            CancellationToken cancellationToken = default) => Task.FromResult<FavoriteCandidate?>(null);
        public Task<bool> ExistsAsync(Guid sourcePetId, Guid candidatePetId,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<(IReadOnlyCollection<FavoriteCandidate> Items, int TotalItems)> GetPagedAsync(
            Guid sourcePetId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyCollection<FavoriteCandidate>)[], 0));
        public void Add(FavoriteCandidate favorite) { }
        public void Remove(FavoriteCandidate favorite) { }
    }
}
