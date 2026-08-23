using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Health.Domain.Errors;

public static class VaccinationErrors
{
    public static readonly Error VaccineNotFound = Error.NotFound("Vaccination.VaccineNotFound", "The vaccine does not exist or is inactive.");
    public static readonly Error VaccinationNotFound = Error.NotFound("Vaccination.NotFound", "The vaccination record was not found.");
    public static readonly Error PetNotFound = Error.NotFound("Vaccination.PetNotFound", "The pet was not found.");
    public static readonly Error PetForbidden = Error.Unauthorized("Vaccination.PetForbidden", "The current user does not own this pet.");
    public static readonly Error PetAuthenticationFailed = Error.Unauthorized("Vaccination.PetAuthenticationFailed", "Pets rejected the forwarded authentication token.");
    public static readonly Error PetsServiceUnavailable = Error.Failure("Vaccination.PetsServiceUnavailable", "Pet ownership could not be verified.");
    public static readonly Error VaccineSpeciesMismatch = Error.Validation("VACCINE_SPECIES_MISMATCH", "La vacuna seleccionada no corresponde a la especie de la mascota.");
    public static readonly Error Duplicate = Error.Conflict("Vaccination.Duplicate", "An exact vaccination record already exists.");
    public static readonly Error InvalidPetId = Error.Validation("Vaccination.InvalidPetId", "PetId is required.");
    public static readonly Error InvalidSpeciesId = Error.Validation("Vaccination.InvalidSpeciesId", "SpeciesId must be 1 (dog) or 2 (cat).");
    public static readonly Error InvalidDoseNumber = Error.Validation("Vaccination.InvalidDoseNumber", "DoseNumber must be greater than zero when provided.");
    public static readonly Error AppliedAtTooFarInFuture = Error.Validation("Vaccination.AppliedAtTooFarInFuture", "AppliedAtUtc cannot be more than five minutes in the future.");
    public static readonly Error InvalidAppliedAt = Error.Validation("Vaccination.InvalidAppliedAt", "AppliedAtUtc must be a valid UTC date.");
    public static readonly Error InvalidFieldLength = Error.Validation("Vaccination.InvalidFieldLength", "One or more text fields exceed their maximum length.");
}
