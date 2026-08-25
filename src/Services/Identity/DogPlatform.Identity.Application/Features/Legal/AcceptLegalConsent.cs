using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Legal;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Legal;

public sealed record AcceptLegalConsentCommand(Guid UserId, Guid LegalDocumentId)
    : IRequest<Result<LegalConsentHistoryResponse>>;

internal sealed class AcceptLegalConsentCommandHandler(
    ILegalDocumentRepository documents,
    IUserLegalConsentRepository consents,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<AcceptLegalConsentCommand, Result<LegalConsentHistoryResponse>>
{
    public async Task<Result<LegalConsentHistoryResponse>> Handle(
        AcceptLegalConsentCommand request, CancellationToken cancellationToken)
    {
        var document = await documents.GetActiveByIdAsync(request.LegalDocumentId, cancellationToken);
        if (document is null || !document.RequiresAcceptance)
            return Result.Failure<LegalConsentHistoryResponse>(LegalErrors.DocumentNotFound);

        if (await consents.ExistsAsync(request.UserId, request.LegalDocumentId, cancellationToken))
            return Result.Failure<LegalConsentHistoryResponse>(LegalErrors.ConsentAlreadyExists);

        var acceptedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var consent = UserLegalConsent.Accept(Guid.NewGuid(), request.UserId,
            request.LegalDocumentId, acceptedAtUtc);
        await consents.AddAsync(consent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LegalConsentHistoryResponse(
            document.Type.ToString(), document.Version, acceptedAtUtc, null));
    }
}
