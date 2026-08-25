using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Legal;

public sealed class LegalDocument : AggregateRoot<Guid>
{
    private LegalDocument(Guid id, LegalDocumentType type, string version, string title,
        string content, DateTime publishedAtUtc, DateTime effectiveAtUtc,
        bool isActive, bool requiresAcceptance, DateTime createdAtUtc) : base(id)
    {
        Type = type;
        Version = version;
        Title = title;
        Content = content;
        PublishedAtUtc = publishedAtUtc;
        EffectiveAtUtc = effectiveAtUtc;
        IsActive = isActive;
        RequiresAcceptance = requiresAcceptance;
        CreatedAtUtc = createdAtUtc;
    }

    private LegalDocument() { }

    public LegalDocumentType Type { get; private set; }
    public string Version { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime PublishedAtUtc { get; private set; }
    public DateTime EffectiveAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public bool RequiresAcceptance { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public static LegalDocument Create(Guid id, LegalDocumentType type, string version,
        string title, string content, DateTime publishedAtUtc, DateTime effectiveAtUtc,
        bool isActive, bool requiresAcceptance, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return new LegalDocument(id, type, version.Trim(), title.Trim(), content,
            publishedAtUtc, effectiveAtUtc, isActive, requiresAcceptance, createdAtUtc);
    }
}
