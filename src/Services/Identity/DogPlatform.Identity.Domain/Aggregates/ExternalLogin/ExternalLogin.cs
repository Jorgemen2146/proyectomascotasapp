using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Aggregates.ExternalLogin;

public sealed class ExternalLogin : Entity<Guid>
{
    private ExternalLogin(Guid id, Guid userId, ExternalAuthProvider provider,
        string providerUserId, string? emailAtLinkTime, DateTime createdAtUtc) : base(id)
    {
        UserId = userId;
        Provider = provider;
        ProviderUserId = providerUserId;
        EmailAtLinkTime = emailAtLinkTime;
        CreatedAtUtc = createdAtUtc;
    }

    private ExternalLogin() { }

    public Guid UserId { get; private set; }
    public ExternalAuthProvider Provider { get; private set; }
    public string ProviderUserId { get; private set; } = string.Empty;
    public string? EmailAtLinkTime { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public static ExternalLogin Create(Guid userId, ExternalAuthProvider provider,
        string providerUserId, string? emailAtLinkTime, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerUserId);
        return new ExternalLogin(Guid.NewGuid(), userId, provider,
            providerUserId.Trim(), emailAtLinkTime?.Trim().ToLowerInvariant(), utcNow);
    }
}
