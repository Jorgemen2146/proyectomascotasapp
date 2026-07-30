using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Pets.Domain.Errors;

public static class SpeciesErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Species.NotFound", "The specified species does not exist.");
}
