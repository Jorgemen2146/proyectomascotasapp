using FluentValidation;

namespace DogPlatform.Matching.Application.Features.SearchCandidates;

public sealed class SearchCandidatesQueryValidator : AbstractValidator<SearchCandidatesQuery>
{
    public SearchCandidatesQueryValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
