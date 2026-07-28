using DogPlatform.Identity.Domain.Errors;
using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Aggregates.RefreshToken;

public sealed class RefreshToken : AggregateRoot<Guid>
{
    private RefreshToken(
        Guid id,
        Guid userId,
        string token,
        DateTime expiresAt,
        DateTime createdAt)
        : base(id)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    // Required for ORM hydration.
    private RefreshToken() { }

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // ── Computed state ───────────────────────────────────────────────────────

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpired(DateTime utcNow) => ExpiresAt < utcNow;

    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    // ── Factory ──────────────────────────────────────────────────────────────

    public static RefreshToken Create(
        Guid id,
        Guid userId,
        string token,
        DateTime expiresAt,
        DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return new RefreshToken(id, userId, token, expiresAt, createdAt);
    }

    // ── Behavior ─────────────────────────────────────────────────────────────

    public Result Revoke(DateTime utcNow)
    {
        if (IsRevoked)
            return Result.Failure(RefreshTokenErrors.AlreadyRevoked);

        RevokedAt = utcNow;
        return Result.Success();
    }
}
