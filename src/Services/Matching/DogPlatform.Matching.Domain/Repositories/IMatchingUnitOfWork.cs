namespace DogPlatform.Matching.Domain.Repositories;

/// <summary>
/// Unit of work abstraction for committing changes across Matching repositories.
/// </summary>
public interface IMatchingUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
