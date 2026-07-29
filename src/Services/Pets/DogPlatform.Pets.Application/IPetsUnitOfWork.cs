namespace DogPlatform.Pets.Application;

public interface IPetsUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
