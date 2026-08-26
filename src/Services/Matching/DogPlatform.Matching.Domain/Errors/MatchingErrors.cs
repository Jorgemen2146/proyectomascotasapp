using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Matching.Domain.Errors;

public static class MatchingErrors
{
    public static readonly Error MatchingProfileNotFound =
        Error.NotFound("MATCHING_PROFILE_NOT_FOUND", "The matching profile was not found.");
    public static readonly Error MatchingProfileInactive =
        Error.Validation("MATCHING_PROFILE_INACTIVE", "The matching profile is inactive.");
    public static readonly Error MatchingRequestExists =
        Error.Conflict("MATCHING_REQUEST_EXISTS", "A pending request already exists between these pets.");
    public static readonly Error MatchingSelfRequest =
        Error.Validation("MATCHING_SELF_REQUEST", "A request cannot target the same pet or owner.");
    public static readonly Error MatchingNotCompatible =
        Error.Validation("MATCHING_NOT_COMPATIBLE", "The pets do not meet the configured compatibility rules.");
    public static readonly Error Forbidden =
        Error.Unauthorized("MATCHING_FORBIDDEN", "You are not authorized to perform this action.");
    public static readonly Error RequestAlreadyProcessed =
        Error.Conflict("MATCHING_REQUEST_ALREADY_PROCESSED", "The request has already been processed.");
    public static readonly Error MatchNotAccepted =
        Error.Conflict("MATCHING_MATCH_NOT_ACCEPTED", "Contact is available only for an accepted match.");
    public static readonly Error ContactNotShared =
        Error.Validation("MATCHING_CONTACT_NOT_SHARED", "The requested contact field was not shared.");
    public static readonly Error RelatedPetsWarning =
        Error.Validation("MATCHING_RELATED_PETS_WARNING", "A known genealogical relationship exists between the pets.");
    public static readonly Error MatchNotFound =
        Error.NotFound("Matching.MatchNotFound", "The accepted match was not found.");
    public static readonly Error BreedingIntentNotFound =
        Error.NotFound("Matching.BreedingIntentNotFound", "The breeding intent was not found.");
    public static readonly Error BreedingIntentExists =
        Error.Conflict("MATCHING_BREEDING_INTENT_EXISTS", "An open breeding intent already exists for this match.");
    public static readonly Error BreedingIntentNotesTooLong =
        Error.Validation("Matching.BreedingIntentNotesTooLong", "Notes cannot exceed 1000 characters.");
    public static readonly Error InvalidLookingForSex =
        Error.Validation("MatchingProfile.InvalidLookingForSex", "LookingForSex must be M or F.");
    public static readonly Error ProfileDescriptionTooLong =
        Error.Validation("MatchingProfile.DescriptionTooLong", "Description cannot exceed 1000 characters.");

    public static readonly Error ProfileNotFound = MatchingProfileNotFound;

    public static readonly Error ProfileNotActive = MatchingProfileInactive;

    public static readonly Error Unauthorized = Forbidden;

    public static readonly Error InvalidAgeRange =
        Error.Validation("MatchingProfile.InvalidAgeRange", "MinimumAgeMonths cannot be greater than MaximumAgeMonths.");

    public static readonly Error InvalidInbreedingCoefficient =
        Error.Validation("MatchingProfile.InvalidInbreedingCoefficient", "MaximumEstimatedInbreedingCoefficient must be between 0 and 1.");

    public static readonly Error InvalidScore =
        Error.Validation("MatchingProfile.InvalidScore", "MinimumCompatibilityScore must be between 0 and 100.");

    public static readonly Error PetNotFound =
        Error.NotFound("Matching.PetNotFound", "The pet was not found.");

    public static readonly Error PetDeleted =
        Error.Validation("Matching.PetDeleted", "The pet is deleted and cannot be used for matching.");

    public static readonly Error CandidateNotFound =
        Error.NotFound("Matching.CandidateNotFound", "The candidate pet was not found or is not a valid match candidate.");

    public static readonly Error CandidateNotEligible = MatchingNotCompatible;

    public static readonly Error SamePet = MatchingSelfRequest;

    public static readonly Error RequestNotFound =
        Error.NotFound("MatchRequest.NotFound", "The match request was not found.");

    public static readonly Error RequestNotPending = RequestAlreadyProcessed;

    public static readonly Error RequestAlreadyFinalized = RequestAlreadyProcessed;

    public static readonly Error DuplicateActiveRequest = MatchingRequestExists;

    public static readonly Error MessageTooLong =
        Error.Validation("MatchRequest.MessageTooLong", "Message cannot exceed 500 characters.");

    public static readonly Error MessageContainsContactInfo =
        Error.Validation("MatchRequest.MessageContainsContactInfo", "Message cannot contain emails, phone numbers, or URLs.");

    public static readonly Error DuplicateFavorite =
        Error.Conflict("Favorite.Duplicate", "This candidate is already in your favorites.");

    public static readonly Error FavoriteNotFound =
        Error.NotFound("Favorite.NotFound", "The favorite was not found.");

    public static readonly Error GenealogyValidationRequired =
        Error.Validation("Matching.GenealogyValidationRequired", "Genealogy validation is required but the service is unavailable.");

    public static readonly Error PetsServiceUnavailable =
        Error.Failure("Matching.PetsServiceUnavailable", "PetsService is currently unavailable.");
}
