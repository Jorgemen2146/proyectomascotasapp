using FluentValidation;

namespace DogPlatform.Genealogy.Application.Features.GetSiblings;

public sealed class GetSiblingsQueryValidator : AbstractValidator<GetSiblingsQuery>
{
    public GetSiblingsQueryValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty().WithMessage("PetId is required.");
    }
}
