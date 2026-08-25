using DogPlatform.Identity.Domain.Legal;

namespace DogPlatform.Identity.Domain.Repositories;

public interface ILegalDocumentRepository
{
    Task<IReadOnlyList<LegalDocument>> GetActiveRequiredAsync(CancellationToken cancellationToken = default);
    Task<LegalDocument?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LegalDocument>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
