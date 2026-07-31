using FluentValidation;

namespace DogPlatform.Pets.Application.Features.PetPhotos.CreateUploadUrl;

public sealed class CreatePetPhotoUploadUrlCommandValidator
    : AbstractValidator<CreatePetPhotoUploadUrlCommand>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB default (overridden by S3StorageOptions)

    public CreatePetPhotoUploadUrlCommandValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty();

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("ContentType must be image/jpeg, image/png, or image/webp.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .WithMessage("FileSize must be greater than zero.")
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"FileSize must not exceed {MaxFileSizeBytes / 1024 / 1024} MB.");
    }
}
