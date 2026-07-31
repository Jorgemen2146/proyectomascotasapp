using FluentValidation;

namespace DogPlatform.Pets.Application.Features.PetPhotos.ConfirmUpload;

public sealed class ConfirmPetPhotoUploadCommandValidator
    : AbstractValidator<ConfirmPetPhotoUploadCommand>
{
    public ConfirmPetPhotoUploadCommandValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty();

        RuleFor(x => x.ObjectKey)
            .NotEmpty()
            .MaximumLength(1024);
    }
}
