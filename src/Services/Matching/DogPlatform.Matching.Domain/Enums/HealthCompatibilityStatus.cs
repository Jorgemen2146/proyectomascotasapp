namespace DogPlatform.Matching.Domain.Enums;

/// <summary>
/// Placeholder abstraction for a future HealthService integration.
/// In Matching v1, this always evaluates to <see cref="Unknown"/>. No medical
/// compatibility, genetic testing, or health score is inferred or invented.
/// </summary>
public enum HealthCompatibilityStatus
{
    Unknown = 0,
    Compatible = 1,
    Warning = 2,
    Incompatible = 3
}
