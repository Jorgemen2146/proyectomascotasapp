namespace DogPlatform.Genealogy.Domain.Repositories;

public interface IGenealogyUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
