using DogPlatform.Identity.Domain.Aggregates.ExternalLogin;

namespace DogPlatform.Identity.Domain.Repositories;

public interface IExternalLoginRepository
{
    Task<ExternalLogin?> GetAsync(ExternalAuthProvider provider, string providerUserId,
        CancellationToken cancellationToken = default);
    Task AddAsync(ExternalLogin externalLogin, CancellationToken cancellationToken = default);
}
