using DogPlatform.Identity.Domain.Legal;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Identity.Infrastructure.Persistence.Repositories;

internal sealed class LegalDocumentRepository(IdentityDbContext context) : ILegalDocumentRepository
{
    public async Task<IReadOnlyList<LegalDocument>> GetActiveRequiredAsync(
        CancellationToken cancellationToken = default) =>
        await context.LegalDocuments.AsNoTracking()
            .Where(document => document.IsActive && document.RequiresAcceptance)
            .OrderBy(document => document.Type).ThenBy(document => document.EffectiveAtUtc)
            .ToListAsync(cancellationToken);

    public Task<LegalDocument?> GetActiveByIdAsync(Guid id,
        CancellationToken cancellationToken = default) =>
        context.LegalDocuments.AsNoTracking()
            .FirstOrDefaultAsync(document => document.Id == id && document.IsActive,
                cancellationToken);

    public async Task<IReadOnlyList<LegalDocument>> GetByIdsAsync(IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var documentIds = ids.Distinct().ToArray();
        return await context.LegalDocuments.AsNoTracking()
            .Where(document => documentIds.Contains(document.Id))
            .ToListAsync(cancellationToken);
    }
}
