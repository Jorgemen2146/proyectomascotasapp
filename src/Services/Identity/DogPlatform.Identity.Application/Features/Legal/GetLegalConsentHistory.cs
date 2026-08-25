using DogPlatform.Identity.Domain.Repositories;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Legal;

public sealed record GetLegalConsentHistoryQuery(Guid UserId)
    : IRequest<IReadOnlyList<LegalConsentHistoryResponse>>;

internal sealed class GetLegalConsentHistoryQueryHandler(
    ILegalDocumentRepository documents,
    IUserLegalConsentRepository consents)
    : IRequestHandler<GetLegalConsentHistoryQuery, IReadOnlyList<LegalConsentHistoryResponse>>
{
    public async Task<IReadOnlyList<LegalConsentHistoryResponse>> Handle(
        GetLegalConsentHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await consents.GetByUserIdAsync(request.UserId, cancellationToken);
        var legalDocuments = await documents.GetByIdsAsync(
            history.Select(consent => consent.LegalDocumentId), cancellationToken);
        var byId = legalDocuments.ToDictionary(document => document.Id);

        return history
            .Where(consent => byId.ContainsKey(consent.LegalDocumentId))
            .OrderByDescending(consent => consent.AcceptedAtUtc)
            .Select(consent => new LegalConsentHistoryResponse(
                byId[consent.LegalDocumentId].Type.ToString(),
                byId[consent.LegalDocumentId].Version,
                consent.AcceptedAtUtc,
                consent.RevokedAtUtc))
            .ToList();
    }
}
