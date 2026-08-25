using DogPlatform.Identity.Domain.Repositories;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Legal;

public sealed record GetLegalStatusQuery(Guid UserId) : IRequest<LegalStatusResponse>;

internal sealed class GetLegalStatusQueryHandler(
    ILegalDocumentRepository documents,
    IUserLegalConsentRepository consents)
    : IRequestHandler<GetLegalStatusQuery, LegalStatusResponse>
{
    public async Task<LegalStatusResponse> Handle(
        GetLegalStatusQuery request, CancellationToken cancellationToken)
    {
        var required = await documents.GetActiveRequiredAsync(cancellationToken);
        var history = await consents.GetByUserIdAsync(request.UserId, cancellationToken);
        var acceptedIds = history
            .Where(consent => consent.RevokedAtUtc is null)
            .Select(consent => consent.LegalDocumentId)
            .ToHashSet();

        var pending = required
            .Where(document => !acceptedIds.Contains(document.Id))
            .Select(LegalDocumentResponse.From)
            .ToList();

        return new LegalStatusResponse(pending.Count == 0, pending);
    }
}
