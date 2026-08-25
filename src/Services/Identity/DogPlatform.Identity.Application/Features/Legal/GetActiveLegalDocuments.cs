using DogPlatform.Identity.Domain.Repositories;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Legal;

public sealed record GetActiveLegalDocumentsQuery : IRequest<IReadOnlyList<LegalDocumentResponse>>;

internal sealed class GetActiveLegalDocumentsQueryHandler(ILegalDocumentRepository documents)
    : IRequestHandler<GetActiveLegalDocumentsQuery, IReadOnlyList<LegalDocumentResponse>>
{
    public async Task<IReadOnlyList<LegalDocumentResponse>> Handle(
        GetActiveLegalDocumentsQuery request, CancellationToken cancellationToken)
    {
        var active = await documents.GetActiveRequiredAsync(cancellationToken);
        return active.Select(LegalDocumentResponse.From).ToList();
    }
}
