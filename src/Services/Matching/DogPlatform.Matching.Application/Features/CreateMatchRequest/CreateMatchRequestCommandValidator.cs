using System.Text.RegularExpressions;
using FluentValidation;

namespace DogPlatform.Matching.Application.Features.CreateMatchRequest;

public sealed partial class CreateMatchRequestCommandValidator : AbstractValidator<CreateMatchRequestCommand>
{
    [GeneratedRegex(@"[\w\.-]+@[\w\.-]+\.\w+|(\+?\d[\d\-\s]{6,}\d)|(https?:\/\/|www\.)\S+",
        RegexOptions.IgnoreCase)]
    private static partial Regex ContactInfoPattern();

    public CreateMatchRequestCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.CandidatePetId).NotEmpty();
        RuleFor(x => x.PetId)
            .NotEqual(x => x.CandidatePetId)
            .WithMessage("A pet cannot request a match with itself.");

        RuleFor(x => x.Message)
            .MaximumLength(500)
            .Must(message => message is null || !ContactInfoPattern().IsMatch(message))
            .WithMessage("Message cannot contain emails, phone numbers, or URLs.");
    }
}
