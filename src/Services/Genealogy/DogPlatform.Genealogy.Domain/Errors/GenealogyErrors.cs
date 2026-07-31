using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Genealogy.Domain.Errors;

public static class GenealogyErrors
{
    public static readonly Error PetCannotBeItsOwnFather =
        Error.Validation(
            "Genealogy.PetCannotBeItsOwnFather",
            "A pet cannot be assigned as its own father.");

    public static readonly Error PetCannotBeItsOwnMother =
        Error.Validation(
            "Genealogy.PetCannotBeItsOwnMother",
            "A pet cannot be assigned as its own mother.");

    public static readonly Error FatherAndMotherCannotBeTheSamePet =
        Error.Validation(
            "Genealogy.FatherAndMotherCannotBeTheSamePet",
            "The father and mother cannot be the same pet.");

    public static readonly Error LineageNotFound =
        Error.NotFound(
            "Genealogy.LineageNotFound",
            "No lineage record was found for the specified pet.");

    public static readonly Error PetNotFound =
        Error.NotFound(
            "Genealogy.PetNotFound",
            "The specified pet does not exist.");

    public static readonly Error FatherNotFound =
        Error.NotFound(
            "Genealogy.FatherNotFound",
            "The specified father pet does not exist.");

    public static readonly Error MotherNotFound =
        Error.NotFound(
            "Genealogy.MotherNotFound",
            "The specified mother pet does not exist.");

    public static readonly Error Unauthorized =
        Error.Unauthorized(
            "Genealogy.Unauthorized",
            "You are not the owner of this pet.");
}
