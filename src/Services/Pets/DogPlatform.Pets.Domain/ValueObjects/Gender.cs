using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Pets.Domain.ValueObjects;

public sealed class Gender : ValueObject
{
    public const string Male = "M";
    public const string Female = "F";

    private Gender(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Gender> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Gender>(Error.Validation("Gender.Empty", "Gender is required."));

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized != Male && normalized != Female)
            return Result.Failure<Gender>(Error.Validation("Gender.Invalid", "Gender must be 'M' or 'F'."));

        return Result.Success(new Gender(normalized));
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
