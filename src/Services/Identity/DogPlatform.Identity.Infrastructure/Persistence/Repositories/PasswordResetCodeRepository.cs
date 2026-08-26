using DogPlatform.Identity.Domain.Aggregates.PasswordResetCode;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Identity.Infrastructure.Persistence.Repositories;

internal sealed class PasswordResetCodeRepository(IdentityDbContext context)
    : IPasswordResetCodeRepository
{
    public async Task<PasswordResetCode?> GetLatestByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await context.PasswordResetCodes
            .Where(code => code.UserId == userId)
            .OrderByDescending(code => code.CreatedAtUtc)
            .ThenByDescending(code => code.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<PasswordResetCode>> GetPendingByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await context.PasswordResetCodes
            .Where(code => code.UserId == userId && code.UsedAtUtc == null && !code.IsRevoked)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        PasswordResetCode code, CancellationToken cancellationToken = default) =>
        await context.PasswordResetCodes.AddAsync(code, cancellationToken);
}
