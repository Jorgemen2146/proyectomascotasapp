using DogPlatform.Identity.Domain.Aggregates.ExternalLogin;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Identity.Infrastructure.Persistence.Repositories;

internal sealed class ExternalLoginRepository(IdentityDbContext context) : IExternalLoginRepository
{
    public Task<ExternalLogin?> GetAsync(ExternalAuthProvider provider, string providerUserId,
        CancellationToken cancellationToken = default) =>
        context.ExternalLogins.FirstOrDefaultAsync(x => x.Provider == provider
            && x.ProviderUserId == providerUserId, cancellationToken);

    public async Task AddAsync(ExternalLogin externalLogin,
        CancellationToken cancellationToken = default) =>
        await context.ExternalLogins.AddAsync(externalLogin, cancellationToken);
}
