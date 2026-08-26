using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Aggregates.PasswordResetCode;

public sealed class PasswordResetCode : AggregateRoot<Guid>
{
    private PasswordResetCode(
        Guid id,
        Guid userId,
        string codeHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        string? createdFromIp)
        : base(id)
    {
        UserId = userId;
        CodeHash = codeHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        CreatedFromIp = createdFromIp;
    }

    private PasswordResetCode() { }

    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public int FailedAttempts { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? CreatedFromIp { get; private set; }

    public static PasswordResetCode Create(
        Guid id,
        Guid userId,
        string codeHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        string? createdFromIp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);
        if (expiresAtUtc <= createdAtUtc)
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc));

        return new PasswordResetCode(
            id, userId, codeHash, createdAtUtc, expiresAtUtc, createdFromIp?.Trim());
    }

    public bool IsExpired(DateTime utcNow) => ExpiresAtUtc <= utcNow;

    public bool IsLocked(int maximumAttempts) =>
        IsRevoked || FailedAttempts >= maximumAttempts;

    public bool RecordFailedAttempt(int maximumAttempts)
    {
        if (UsedAtUtc.HasValue || IsRevoked)
            return true;

        FailedAttempts++;
        if (FailedAttempts >= maximumAttempts)
            IsRevoked = true;

        return IsRevoked;
    }

    public void Revoke() => IsRevoked = true;

    public void MarkUsed(DateTime utcNow)
    {
        if (UsedAtUtc.HasValue || IsRevoked)
            throw new InvalidOperationException("The password reset code is no longer active.");

        UsedAtUtc = utcNow;
    }
}
