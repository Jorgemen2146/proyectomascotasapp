namespace DogPlatform.Identity.Application;

public interface IIdentityUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
