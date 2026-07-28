namespace DogPlatform.SharedKernel.Primitives;

/// <summary>
/// Classifies the nature of an error for use in Results.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4
}

/// <summary>
/// Represents a typed, structured error.
/// Used with Result to avoid throwing exceptions for expected failure paths.
/// </summary>
public sealed class Error
{
    private Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }

    // ── Factory methods ──────────────────────────────────────────────────────

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    // ── Well-known errors ────────────────────────────────────────────────────

    /// <summary>Represents the absence of an error. Used by Result.Success().</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public override string ToString() => $"[{Type}] {Code}: {Description}";
}
