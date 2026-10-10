using DogPlatform.Identity.Domain.Aggregates.User;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _context.Users.AddAsync(user, cancellationToken);

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email.Value == email.Value, cancellationToken);

    public async Task<IReadOnlyCollection<User>> SearchActiveAsync(string query, int limit,
        CancellationToken cancellationToken = default)
    {
        var term = query.Trim();
        return await _context.Users.AsNoTracking()
            .Where(u => u.IsActive &&
                (u.FullName.FirstName.Contains(term) || u.FullName.LastName.Contains(term)))
            .OrderBy(u => u.FullName.FirstName).ThenBy(u => u.FullName.LastName)
            .Take(limit).ToListAsync(cancellationToken);
    }
}
