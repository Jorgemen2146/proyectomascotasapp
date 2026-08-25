using DogPlatform.Identity.Domain.Legal;

namespace DogPlatform.Identity.Application.Features.Legal;

public sealed record LegalConsentSelection(string Type, string Version);

public sealed record LegalDocumentResponse(
    Guid LegalDocumentId,
    string Type,
    string Version,
    string Title,
    string Content,
    DateTime PublishedAtUtc,
    DateTime EffectiveAtUtc,
    bool RequiresAcceptance)
{
    internal static LegalDocumentResponse From(LegalDocument document) => new(
        document.Id,
        document.Type.ToString(),
        document.Version,
        document.Title,
        document.Content,
        document.PublishedAtUtc,
        document.EffectiveAtUtc,
        document.RequiresAcceptance);
}

public sealed record LegalStatusResponse(
    bool IsUpToDate,
    IReadOnlyList<LegalDocumentResponse> PendingDocuments);

public sealed record LegalConsentHistoryResponse(
    string Type,
    string Version,
    DateTime AcceptedAtUtc,
    DateTime? RevokedAtUtc);
