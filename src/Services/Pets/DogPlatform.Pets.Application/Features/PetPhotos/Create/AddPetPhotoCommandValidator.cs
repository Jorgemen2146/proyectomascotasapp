using FluentValidation;

namespace DogPlatform.Pets.Application.Features.PetPhotos.Create;

public sealed class AddPetPhotoCommandValidator : AbstractValidator<AddPetPhotoCommand>
{
    public AddPetPhotoCommandValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty();

        RuleFor(x => x.ImageUrl)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("ImageUrl must be a valid absolute URL.");
    }
}
