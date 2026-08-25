using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Genealogy.Domain.Errors;

public static class GenealogyErrors
{
    public static readonly Error RelationshipExists = Error.Conflict(
        "GENEALOGY_RELATIONSHIP_EXISTS", "The relationship already exists.");
    public static readonly Error ParentAlreadyAssigned = Error.Conflict(
        "GENEALOGY_PARENT_ALREADY_ASSIGNED", "A parent is already assigned for that role.");
    public static readonly Error CycleDetected = Error.Validation(
        "GENEALOGY_CYCLE_DETECTED", "The relationship would create a genealogy cycle.");
    public static readonly Error SelfRelationship = Error.Validation(
        "GENEALOGY_SELF_RELATIONSHIP", "A pet cannot be its own parent.");
    public static readonly Error ParentSexMismatch = Error.Validation(
        "GENEALOGY_PARENT_SEX_MISMATCH", "The selected pet sex does not match the parent role.");
    public static readonly Error InvitationExpired = Error.Validation(
        "GENEALOGY_INVITATION_EXPIRED", "The invitation has expired.");
    public static readonly Error InvitationInvalid = Error.NotFound(
        "GENEALOGY_INVITATION_INVALID", "The invitation is invalid.");
    public static readonly Error InvitationAlreadyProcessed = Error.Conflict(
        "GENEALOGY_INVITATION_ALREADY_PROCESSED", "The invitation was already processed.");
    public static readonly Error Forbidden = Error.Unauthorized(
        "GENEALOGY_FORBIDDEN", "The authenticated user cannot perform this operation.");
    public static readonly Error InvitationAlreadyPending = Error.Conflict(
        "GENEALOGY_INVITATION_ALREADY_PENDING", "An equivalent pending invitation already exists.");
    public static readonly Error InvalidParentRole = Error.Validation(
        "GENEALOGY_PARENT_ROLE_INVALID", "ParentRole must be Father or Mother.");
    public static readonly Error InvalidEmail = Error.Validation(
        "GENEALOGY_INVITATION_EMAIL_INVALID", "A valid owner email is required.");
    public static readonly Error InvalidGenerations = Error.Validation(
        "GENEALOGY_GENERATIONS_INVALID", "Generations must be between 1 and 5.");
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

    public static readonly Error CircularLineageDetected =
        Error.Validation(
            "Genealogy.CircularLineageDetected",
            "The requested parent assignment would create a circular lineage (a pet cannot be its own ancestor or descendant).");

    public static readonly Error MaximumTraversalExceeded =
        Error.Failure(
            "Genealogy.MaximumTraversalExceeded",
            "The lineage graph exceeds the maximum number of nodes allowed for a safe traversal. Please review the underlying data for corruption.");

    public static readonly Error InvalidDepth =
        Error.Validation(
            "Genealogy.InvalidDepth",
            "The requested depth is outside the allowed range.");
}
