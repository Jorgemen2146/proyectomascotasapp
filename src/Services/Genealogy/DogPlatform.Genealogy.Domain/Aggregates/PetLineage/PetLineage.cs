using DogPlatform.Genealogy.Domain.Errors;
using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Genealogy.Domain.Aggregates.PetLineage;

/// <summary>
/// Aggregate that tracks the direct parent relationships of a pet.
/// A pet may have no parents, only a father, only a mother, or both.
/// Future versions will support ancestor traversal and inbreeding detection.
/// </summary>
public sealed class PetLineage : AggregateRoot<Guid>
{
    private PetLineage(
        Guid id,
        Guid petId,
        Guid ownerId,
        Guid? fatherId,
        Guid? motherId,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        PetId    = petId;
        OwnerId  = ownerId;
        FatherId = fatherId;
        MotherId = motherId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    // Required by EF Core
    private PetLineage() { }

    public Guid    PetId     { get; private set; }
    public Guid    OwnerId   { get; private set; }
    public Guid?   FatherId  { get; private set; }
    public Guid?   MotherId  { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────

    public static Result<PetLineage> Create(
        Guid petId,
        Guid ownerId,
        Guid? fatherId,
        Guid? motherId,
        DateTime now)
    {
        var validation = Validate(petId, fatherId, motherId);
        if (validation.IsFailure)
            return Result.Failure<PetLineage>(validation.Error);

        return Result.Success(new PetLineage(
            Guid.NewGuid(),
            petId,
            ownerId,
            fatherId,
            motherId,
            now,
            now));
    }

    // ── Behaviour ──────────────────────────────────────────────────────────

    /// <summary>
    /// Assigns or replaces both parents at once.
    /// Null means "remove that parent".
    /// </summary>
    public Result AssignParents(Guid? fatherId, Guid? motherId, DateTime now)
    {
        var validation = Validate(PetId, fatherId, motherId);
        if (validation.IsFailure)
            return validation;

        FatherId  = fatherId;
        MotherId  = motherId;
        UpdatedAt = now;
        return Result.Success();
    }

    /// <summary>Removes the father relationship.</summary>
    public Result RemoveFather(DateTime now)
    {
        FatherId  = null;
        UpdatedAt = now;
        return Result.Success();
    }

    /// <summary>Removes the mother relationship.</summary>
    public Result RemoveMother(DateTime now)
    {
        MotherId  = null;
        UpdatedAt = now;
        return Result.Success();
    }

    // ── Private validation ─────────────────────────────────────────────────

    private static Result Validate(Guid petId, Guid? fatherId, Guid? motherId)
    {
        if (fatherId.HasValue && fatherId.Value == petId)
            return Result.Failure(GenealogyErrors.PetCannotBeItsOwnFather);

        if (motherId.HasValue && motherId.Value == petId)
            return Result.Failure(GenealogyErrors.PetCannotBeItsOwnMother);

        if (fatherId.HasValue && motherId.HasValue && fatherId.Value == motherId.Value)
            return Result.Failure(GenealogyErrors.FatherAndMotherCannotBeTheSamePet);

        return Result.Success();
    }
}
