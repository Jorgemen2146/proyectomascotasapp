using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Pets.Domain.Errors;

public static class PetErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Pet.NotFound", "The pet was not found.");

    public static readonly Error Unauthorized =
        Error.Unauthorized("Pet.Unauthorized", "You do not have permission to access this pet.");

    public static readonly Error AlreadyDeleted =
        Error.NotFound("Pet.AlreadyDeleted", "The pet has already been deleted.");

    public static readonly Error BreedNotFound =
        Error.NotFound("Pet.BreedNotFound", "The specified breed does not exist.");

    public static readonly Error PhotoNotFound =
        Error.NotFound("Pet.Photo.NotFound", "The photo was not found.");

    public static readonly Error InvalidObjectKey =
        Error.Validation("Pet.Photo.InvalidObjectKey", "The object key does not belong to this pet or user.");

    public static readonly Error ObjectNotFound =
        Error.NotFound("Pet.Photo.ObjectNotFound", "The uploaded object was not found in storage. Upload may have failed or expired.");

    public static readonly Error DuplicatePhoto =
        Error.Conflict("Pet.Photo.Duplicate", "This photo has already been registered.");
}
