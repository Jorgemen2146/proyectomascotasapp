using FluentValidation;

namespace DogPlatform.Pets.Application.Features.Pets.Create;

public sealed class CreatePetValidator : AbstractValidator<CreatePetCommand>
{
    public CreatePetValidator()
    {
        RuleFor(x => x.BreedId)
            .GreaterThan(0)
            .WithMessage("BreedId must be greater than 0.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Gender)
            .NotEmpty()
            .Must(g => g == "M" || g == "F")
            .WithMessage("Gender must be 'M' or 'F'.");

        RuleFor(x => x.Weight)
            .GreaterThan(0)
            .When(x => x.Weight.HasValue);

        RuleFor(x => x.Color)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Color));

        RuleFor(x => x.PedigreeNumber)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.PedigreeNumber));

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
