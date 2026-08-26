using DogPlatform.Identity.Domain.Aggregates.PasswordResetCode;

namespace DogPlatform.Identity.Domain.Repositories;

public interface IPasswordResetCodeRepository
{
    Task<PasswordResetCode?> GetLatestByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PasswordResetCode>> GetPendingByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(PasswordResetCode code, CancellationToken cancellationToken = default);
}
