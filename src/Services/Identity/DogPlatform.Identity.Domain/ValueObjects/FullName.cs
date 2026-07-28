using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.ValueObjects;

public sealed class FullName : ValueObject
{
    public const int MaxLength = 100;

    private FullName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public string FirstName { get; }
    public string LastName { get; }
    public string Display => $"{FirstName} {LastName}";

    public static Result<FullName> Create(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure<FullName>(Errors.FirstNameEmpty);

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure<FullName>(Errors.LastNameEmpty);

        firstName = firstName.Trim();
        lastName = lastName.Trim();

        if (firstName.Length > MaxLength)
            return Result.Failure<FullName>(Errors.FirstNameTooLong);

        if (lastName.Length > MaxLength)
            return Result.Failure<FullName>(Errors.LastNameTooLong);

        return Result.Success(new FullName(firstName, lastName));
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return FirstName;
        yield return LastName;
    }

    public override string ToString() => Display;

    public static class Errors
    {
        public static readonly Error FirstNameEmpty =
            Error.Validation("FullName.FirstNameEmpty", "First name cannot be empty.");

        public static readonly Error LastNameEmpty =
            Error.Validation("FullName.LastNameEmpty", "Last name cannot be empty.");

        public static readonly Error FirstNameTooLong =
            Error.Validation("FullName.FirstNameTooLong", $"First name cannot exceed {MaxLength} characters.");

        public static readonly Error LastNameTooLong =
            Error.Validation("FullName.LastNameTooLong", $"Last name cannot exceed {MaxLength} characters.");
    }
}
