using DogPlatform.Identity.Domain.Legal;

namespace DogPlatform.Identity.Domain.Repositories;

public interface IUserLegalConsentRepository
{
    Task<IReadOnlyList<UserLegalConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid legalDocumentId, CancellationToken cancellationToken = default);
    Task AddAsync(UserLegalConsent consent, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<UserLegalConsent> consents, CancellationToken cancellationToken = default);
}
