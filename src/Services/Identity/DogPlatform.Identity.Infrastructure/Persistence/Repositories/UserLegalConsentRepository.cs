using DogPlatform.Identity.Domain.Legal;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserLegalConsentRepository(IdentityDbContext context)
    : IUserLegalConsentRepository
{
    public async Task<IReadOnlyList<UserLegalConsent>> GetByUserIdAsync(Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.UserLegalConsents.AsNoTracking()
            .Where(consent => consent.UserId == userId)
            .OrderByDescending(consent => consent.AcceptedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid userId, Guid legalDocumentId,
        CancellationToken cancellationToken = default) =>
        context.UserLegalConsents.AnyAsync(consent => consent.UserId == userId
            && consent.LegalDocumentId == legalDocumentId, cancellationToken);

    public async Task AddAsync(UserLegalConsent consent,
        CancellationToken cancellationToken = default) =>
        await context.UserLegalConsents.AddAsync(consent, cancellationToken);

    public Task AddRangeAsync(IEnumerable<UserLegalConsent> consents,
        CancellationToken cancellationToken = default)
    {
        context.UserLegalConsents.AddRange(consents);
        return Task.CompletedTask;
    }
}
