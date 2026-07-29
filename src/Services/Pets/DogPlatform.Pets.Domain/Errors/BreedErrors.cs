using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Pets.Domain.Errors;

public static class BreedErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Breed.NotFound", "The breed was not found.");
}
