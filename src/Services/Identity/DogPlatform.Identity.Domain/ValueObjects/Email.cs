using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public const int MaxLength = 200;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Email>(Errors.Empty);

        value = value.Trim().ToLowerInvariant();

        if (value.Length > MaxLength)
            return Result.Failure<Email>(Errors.TooLong);

        int atIndex = value.IndexOf('@');
        if (atIndex <= 0 || atIndex == value.Length - 1)
            return Result.Failure<Email>(Errors.InvalidFormat);

        int dotIndex = value.IndexOf('.', atIndex);
        if (dotIndex < 0 || dotIndex == value.Length - 1)
            return Result.Failure<Email>(Errors.InvalidFormat);

        return Result.Success(new Email(value));
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static class Errors
    {
        public static readonly Error Empty =
            Error.Validation("Email.Empty", "Email address cannot be empty.");

        public static readonly Error TooLong =
            Error.Validation("Email.TooLong", $"Email address cannot exceed {MaxLength} characters.");

        public static readonly Error InvalidFormat =
            Error.Validation("Email.InvalidFormat", "Email address format is invalid.");
    }
}
