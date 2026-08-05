using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Matching.Domain.Errors;

public static class MatchingErrors
{
    public static readonly Error ProfileNotFound =
        Error.NotFound("MatchingProfile.NotFound", "The matching profile was not found.");

    public static readonly Error ProfileNotActive =
        Error.Validation("MatchingProfile.NotActive", "The matching profile is not active.");

    public static readonly Error Unauthorized =
        Error.Unauthorized("Matching.Unauthorized", "You are not authorized to perform this action.");

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

    public static readonly Error CandidateNotEligible =
        Error.Validation("Matching.CandidateNotEligible", "The candidate is not eligible for matching.");

    public static readonly Error SamePet =
        Error.Validation("Matching.SamePet", "A pet cannot be matched or favorited against itself.");

    public static readonly Error RequestNotFound =
        Error.NotFound("MatchRequest.NotFound", "The match request was not found.");

    public static readonly Error RequestNotPending =
        Error.Conflict("MatchRequest.NotPending", "Only pending requests can be accepted or rejected.");

    public static readonly Error RequestAlreadyFinalized =
        Error.Conflict("MatchRequest.AlreadyFinalized", "The request has already been finalized and cannot be cancelled.");

    public static readonly Error DuplicateActiveRequest =
        Error.Conflict("MatchRequest.DuplicateActive", "There is already an active request (Pending or Accepted) between these pets.");

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
